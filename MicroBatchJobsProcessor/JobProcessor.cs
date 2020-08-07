using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MicroBatchJobsProcessor.BatchProcessor;
using MicroBatchJobsProcessor.BatchScheduler;

namespace MicroBatchJobsProcessor
{
    /// <summary>
    /// The main Micro-batching class
    /// Contains two methods: Process(Job) and ShutDown()
    /// Please keep in mind that Shutdown method return once all jobs sent processed
    /// </summary>
    public class JobProcessor
    {
        private readonly IBatchScheduler _batchScheduler;
        private readonly int _maxBatchSize;
        private readonly ConcurrentQueue<ProcessorJob> _processorJobs = new ConcurrentQueue<ProcessorJob>();
        
        //Used in Shutdown logic
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private object batchLock = new object();

        /// <summary>
        /// IBatchProcessor 
        /// Contains two methods: Process(Job) and ShutDown()
        /// Please keep in mind that Shutdown method return once all jobs sent processed
        /// </summary>
        public JobProcessor(IBatchProcessor batchProcessor, int maxBatchSize, TimeSpan maxtimeBetweenCalls)
        {
            Validate(batchProcessor, maxBatchSize, maxtimeBetweenCalls);
            _batchScheduler = new BatchScheduler.BatchScheduler(batchProcessor, maxtimeBetweenCalls);
            _maxBatchSize = maxBatchSize;
        }

        private void Validate(IBatchProcessor batchProcessor, int maxBatchSize, TimeSpan maxtimeBetweenCalls)
        {
            if (batchProcessor == null)
                throw new ApplicationException("IBatchProcessor must be provided");   
            if (maxBatchSize == 0)
                throw new ApplicationException("maxBatchSize must be more than Zero");     
            if (maxtimeBetweenCalls.Milliseconds<1)
                throw new ApplicationException("maxtimeBetweenCalls can be less than 1 ms");
        }

        /// <summary>
        /// Call this method to stop accepting Jobs to process and process already accepted Jobs
        /// </summary>
        public async Task Shutdown()
        {
            _cancellationTokenSource.Cancel();
            ScheduleBatch(true);
            await _batchScheduler.Stop();
        }
        
        /// <summary>
        /// Call this method with Job to process 
        /// </summary>
        public async Task<IJobResult> Process(IJob job)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                throw new ApplicationException("Application shutting down");

            var taskCompletion = new TaskCompletionSource<IJobResult>();
            // Create a processor job with a job and a callback of Completion to return Result
           _processorJobs.Enqueue(
               new ProcessorJob(job, taskCompletion));
           ScheduleBatch();

            return await taskCompletion.Task;
        }
        
        /// <summary>
        /// Each time we enqueue a job we need to check:
        /// 1) if the queue has enough jobs to create a batch
        /// 2) or shutdown method was called and we need to send all the jobs we have as a batch.
        /// </summary>
        private void ScheduleBatch(bool forced = false)
        {
            if (forced || _processorJobs.Count>=_maxBatchSize)
            {
                lock (batchLock)
                {
                    // create and send a batch of batchSize jobs 
                    if ( _processorJobs.Count >= _maxBatchSize)
                    {
                        var batch = new List<ProcessorJob>();
                        for (int i = 1; i <= _maxBatchSize; i++)
                        {
                            if (_processorJobs.TryDequeue(out ProcessorJob job))
                                batch.Add(job);
                            else
                                break;
                        }

                        _batchScheduler.ScheduleBatch(batch);
                    }
                    //if we shutdown we need to put jobs we have in queue
                    if (forced)
                    {
                        var batch = new List<ProcessorJob>();
                        for (int i = 1; i <= _maxBatchSize; i++)
                        {
                            if (_processorJobs.TryDequeue(out ProcessorJob job))
                                batch.Add(job);
                            else
                                break;
                        }

                        _batchScheduler.ScheduleBatch(batch);
                    }
                }
            }
        }
    }
}
