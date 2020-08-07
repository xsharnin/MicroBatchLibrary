using System.Threading.Tasks;
using MicroBatchJobsProcessor.BatchProcessor;

namespace MicroBatchJobsProcessor.BatchScheduler
{
    public class ProcessorJob
    {
        public IJob Job { get; }
        private readonly TaskCompletionSource<IJobResult> _taskCompletionSource;

        public ProcessorJob(IJob job, TaskCompletionSource<IJobResult> taskCompletionSource)
        {
            Job = job;
            _taskCompletionSource = taskCompletionSource;
        }

        public void FinishedWith(IJobResult jobResult)
        {
            _taskCompletionSource.SetResult(jobResult);
        }
    }
}