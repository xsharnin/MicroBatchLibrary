using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MicroBatchJobsProcessor.BatchScheduler
{
    public class Batch
    {
        public Batch()
        {
            Id = Guid.NewGuid();
        }
        public Guid Id{ get; }
        public List<ProcessorJob> Jobs { get; set; }

        public TaskCompletionSource<bool> TaskCompletionSource { get; set; }
    }
}