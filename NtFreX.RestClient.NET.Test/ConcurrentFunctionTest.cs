using System.Collections.Generic;
using System.Threading.Tasks;
using NtFreX.RestClient.NET.Flow;
using Xunit;

namespace NtFreX.RestClient.NET.Test
{
    public class ConcurrentFunctionTest
    {
        [Fact]
        public async Task MaxConcurrencyDoesntGoOverLimit()
        {
            var maxConcurrentCount = 2;
            var concurrentCount = 0;
            async Task ExecuteFuncAsync()
            {
                concurrentCount++;
                Assert.True(concurrentCount <= maxConcurrentCount);
                await Task.Delay(100);
                concurrentCount--;
            }
            
            var fnc = new AsyncConcurrentFunction(ExecuteFuncAsync, maxConcurrentCount);

            var tasks = new List<Task>();
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(async () => await fnc.ExecuteAsync()));
            }
            await Task.WhenAll(tasks);
        }
    }
}
