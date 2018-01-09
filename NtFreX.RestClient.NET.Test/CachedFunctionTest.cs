using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

            var cachingTime = TimeSpan.FromMilliseconds(100);
            var fnc = new CachedFunction<DateTime>(ExecuteFunc, cachingTime);

            Assert.False(fnc.HasCached());

            var firstResult = fnc.Execute();

            Assert.True(fnc.HasCached());
            Assert.Equal(firstResult, fnc.Execute());

            Thread.Sleep(cachingTime + TimeSpan.FromSeconds(1));

            Assert.False(fnc.HasCached());
            Assert.NotEqual(firstResult, fnc.Execute());
        }
    }
}
