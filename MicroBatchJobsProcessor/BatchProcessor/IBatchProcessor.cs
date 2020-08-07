using System.Linq;
using System.Threading.Tasks;

namespace MicroBatchJobsProcessor.BatchProcessor
{
    public interface IBatchProcessor
    {
        Task<IJobResult[]> ProcessBatch(IJob[] jobs);
    }
}
