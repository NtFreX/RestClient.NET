using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NtFreX.RestClient.NET.Extensions;
using NtFreX.RestClient.NET.Flow;
using Xunit;

namespace NtFreX.RestClient.NET.Test
{
    public class CachedFunctionTest
    {
        [Fact]
        public void CachesForTheCorrectTime()
        {
            DateTime ExecuteFunc()
            {
                return DateTime.Now;
            }

            var cachingTime = TimeSpan.FromMilliseconds(1);
            var fnc = new CachedFunction<DateTime>(ExecuteFunc, cachingTime);
            var tasks = new List<Task<DateTime>>();
            var tries = 10;

            for (var i = 0; i < tries; i++)
            {
                tasks.Add(Task.Run(() => fnc.Execute()));
            }
            Task.WaitAll(tasks.ToArray());

            tasks = tasks.OrderBy(x => x.Result).ToList();
            var lastCachingDateTime = DateTime.MinValue;
            for (var i = 0; i < tries; i++)
            {
                if (lastCachingDateTime == DateTime.MinValue)
                {
                    lastCachingDateTime = tasks[i].Result;
                }
                
                if (lastCachingDateTime != tasks[i].Result)
                {
                    Assert.True(tasks[i].Result - lastCachingDateTime >= cachingTime);
                    lastCachingDateTime = tasks[i].Result;
                }
            }
        }
    }
}
