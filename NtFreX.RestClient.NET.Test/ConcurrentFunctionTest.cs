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
            await fnc.ExecuteAsync();
        }
    }
}
