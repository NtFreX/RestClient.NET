using System;
using System.Net.Http;

namespace NtFreX.RestClient.NET
{
    public class RetryStrategy
    {
        public int MaxRetries { get; set; }
        public Func<HttpResponseMessage, bool> RetryWhenResult { get; set; }
        public Func<Exception, bool> RetryWhenException { get; set; }

        public RetryStrategy(int maxTries, Func<HttpResponseMessage, bool> retryWhenResult, Func<Exception, bool> retryWhenException)
        {
            MaxRetries = maxTries;
            RetryWhenResult = retryWhenResult;
            RetryWhenException = retryWhenException;
        }
    }
}