using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MicroBatchJobsProcessor;
using MicroBatchJobsProcessor.BatchProcessor;
using NSubstitute;
using Shouldly;
using TestProject1.Stubs;
using Xunit;

namespace TestProject1
{
    public class JobProcessorTests
    {
        [Theory]
        [InlineData(6, 10, 10)]
        [InlineData(10, 10, 10)]
        [InlineData(11, 10, 10)]
        [InlineData(56, 10, 10)]
        public async Task BatchProcessorExecutionTesting(int jobsCount, int batchSize, int msBetweenCalls)
        {
            var sw = Stopwatch.StartNew();
            var batchProcessor = new StubBatchProcessor();
            var jobProcessor = new JobProcessor(batchProcessor, batchSize, TimeSpan.FromMilliseconds(msBetweenCalls));
            var batchCount = jobsCount / batchSize;
            if (jobsCount - (batchCount * batchSize) > 0)
                batchCount++;

            var tasks = new Task[jobsCount];
            for (int i = 0; i < jobsCount-1; i++)
            {
                tasks[i] = jobProcessor.Process(new StubJob());
            }
            await jobProcessor.Shutdown();
            sw.Stop();
            sw.Elapsed.Milliseconds.ShouldBeGreaterThanOrEqualTo(batchCount*msBetweenCalls);
            batchProcessor.NumberOfExecutions.ShouldBe(batchCount);
        }
        
        [Theory]
        [InlineData(false, true, 0, 10)]
        [InlineData(false, true, 10, 0)]
        [InlineData(false, false, 10, 10)]
        [InlineData(true, true, 10, 10)]
        public void ValidateJobProcessor(bool isValid, bool isBatchProcessorProvided, int batchSize, int msBetweenCalls)
        {
            try
            {
                var batchProcessor = isBatchProcessorProvided ? new StubBatchProcessor() : null;
                new JobProcessor(batchProcessor, batchSize, TimeSpan.FromMilliseconds(msBetweenCalls));
                isValid.ShouldBe(true);
            }
            catch (Exception)
            {
                isValid.ShouldBe(false);
            }
        }
    }
}