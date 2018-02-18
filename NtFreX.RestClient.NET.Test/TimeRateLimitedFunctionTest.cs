using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NtFreX.RestClient.NET.Flow;
using Xunit;

namespace NtFreX.RestClient.NET.Test
{
    public class TimeRateLimitedFunctionTest
    {
        [Fact]
        public async Task IntervalDoesntGoUnderMinInderval()
        {
            DateTime ExecuteFunc()
            {
                return DateTime.Now;   
            }

            var minInterval = TimeSpan.FromMilliseconds(100);
            var fnc = new TimeRateLimitedFunction<DateTime>(ExecuteFunc, minInterval);
            var tasks = new List<Task<DateTime>>();
            var tries = 10;

            for (var i = 0; i < tries; i++)
            {
                tasks.Add(Task.Run(() => fnc.Execute()));
            }
            await Task.WhenAll(tasks.ToArray());

            tasks = tasks.OrderBy(x => x.Result).ToList();
            var currentDateTime = DateTime.MinValue;
            for (var i = 0; i < tries; i++)
            {
                if (currentDateTime != DateTime.MinValue)
                {
                    Assert.True(currentDateTime + minInterval <= tasks[i].Result);
                }
                currentDateTime = tasks[i].Result;
            }
        }
    }
}
