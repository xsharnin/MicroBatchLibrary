using System.Threading.Channels;
using System.Threading.Tasks;
using MicroBatchJobsProcessor.BatchProcessor;

namespace TestProject1.Stubs
{
    public class StubBatchProcessor : IBatchProcessor
    {
        public int NumberOfExecutions { get; private set; }

        public async Task<IJobResult[]> ProcessBatch(IJob[] jobs)
        {
            NumberOfExecutions++;
            return await Task.FromResult(new StubResultJob[jobs.Length]);
        }
    }
}