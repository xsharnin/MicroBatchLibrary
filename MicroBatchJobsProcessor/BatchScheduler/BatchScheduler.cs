using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MicroBatchJobsProcessor.BatchProcessor;

namespace MicroBatchJobsProcessor.BatchScheduler
{
    public interface IBatchScheduler
    {
        void ScheduleBatch(List<ProcessorJob> batch);
        Task Stop();
    }
    
    /// <summary>
    /// BatchScheduler uses external BatchProcessor inside.
    /// This batch has timer which will be executed each *maxTimeBetweenCalls timespan to execute batch of jobs
    /// </summary>
    internal class BatchScheduler : IBatchScheduler
    {
        private readonly IBatchProcessor _batchProcessor;
        private readonly TaskCompletionSource<bool> _taskOfAwaitingBatches = new TaskCompletionSource<bool>();
        private readonly ConcurrentQueue<Batch> _batchesToRun = new ConcurrentQueue<Batch>();
        private readonly ConcurrentDictionary<Guid, Batch> _runningBatches = new ConcurrentDictionary<Guid, Batch>();
        private readonly Timer _batchProcessorTimer;
        private bool _stopping = false;

        public BatchScheduler(IBatchProcessor batchProcessor, TimeSpan maxTimeBetweenCalls)
        {
            _batchProcessor = batchProcessor;
            _batchProcessorTimer = new Timer(ExecuteBatch, null, maxTimeBetweenCalls, maxTimeBetweenCalls);
        }

        public void ScheduleBatch(List<ProcessorJob> batch)
        {
            // Create a batch with jobs and a callback of Completion
            //callback  is on shutdown logic
            _batchesToRun.Enqueue(new Batch
            {
                Jobs = batch,
                TaskCompletionSource = new TaskCompletionSource<bool>()
            });
        }

        private async void ExecuteBatch(object state)
        {
            if (!_batchesToRun.TryDequeue(out Batch batch))
            {
                BatchExecutionReset();
                return;
            }

            _runningBatches.TryAdd(batch.Id, batch);
            
            try
            {
                var processorJobs = batch.Jobs.ToArray();
                var jobs = processorJobs.Select(x => x.Job).ToArray();
                var jobResults = await _batchProcessor.ProcessBatch(jobs);

                for (int i = 0; i < jobResults.Length - 1; i++)
                {
                    processorJobs[i].FinishedWith(jobResults[i]);
                }

                _runningBatches.TryRemove(batch.Id, out batch);

                batch.TaskCompletionSource.SetResult(true);
            }
            catch (Exception e)
            {
                batch.TaskCompletionSource.SetException(e);
            }

            BatchExecutionReset();
        }

        private void BatchExecutionReset()
        {
            if (!_stopping || !_batchesToRun.IsEmpty) return;
            _batchProcessorTimer.Dispose();
            _taskOfAwaitingBatches.TrySetResult(true);
        }


        public async Task Stop()
        {
            _stopping = true;
            await _taskOfAwaitingBatches.Task;
            var tasks = _runningBatches.Values.Select(x => x.TaskCompletionSource.Task).ToArray();
            await Task.WhenAll(tasks);
        }

    }
}