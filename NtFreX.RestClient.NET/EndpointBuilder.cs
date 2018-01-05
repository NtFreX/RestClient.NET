using System;
using System.Collections.Generic;

namespace NtFreX.RestClient.NET
{
    public class EndpointBuilder
    {
        private readonly EndpointSubject _subject;

        public EndpointBuilder(string endpointName, Func<object[], string> uriBuilder)
        {
            _subject = new EndpointSubject(endpointName, uriBuilder);
        }

        public EndpointBuilder WithMaxInterval(TimeSpan maxInterval)
        {
            _subject.MaxInterval = maxInterval;
            return this;
        }

        public EndpointBuilder WithCacheTime(TimeSpan cacheTime)
        {
            _subject.CacheTime = cacheTime;
            return this;
        }

        public EndpointBuilder RetryWhen(int retryCount, params int[] statusCodesToRetry)
        {
            _subject.Retries = retryCount;
            _subject.StatusCodesToRetry = new List<int>(statusCodesToRetry);
            return this;
        }

        public EndpointSubject Build()
            => _subject;
        
        public class EndpointSubject
        {
            public string Name { get; }
            public Func<object[], string> UriBuilder { get; }

            public TimeSpan MaxInterval { get; set; } = TimeSpan.Zero;
            public TimeSpan CacheTime { get; set; } = TimeSpan.Zero;

            public int Retries { get; set; } = 0;
            public List<int> StatusCodesToRetry { get; set; } = new List<int>();

            public EndpointSubject(string name, Func<object[], string> uriBuilder)
            {
                Name = name;
                UriBuilder = uriBuilder;
            }
        }
    }
}