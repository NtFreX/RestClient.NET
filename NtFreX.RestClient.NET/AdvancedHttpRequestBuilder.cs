using System;
using System.Net.Http;
using System.Threading.Tasks;
using NtFreX.RestClient.NET.Flow;

namespace NtFreX.RestClient.NET
{
    public class AdvancedHttpRequestBuilder
    {
        private readonly AdvancedHttpRequestSubject _subject = new AdvancedHttpRequestSubject();

        public AdvancedHttpRequestBuilder Retry(int maxRetries, Func<HttpResponseMessage, bool> retryWhenResult, Func<Exception, bool> retryWhenException)
        {
            _subject.MaxRetryCount = maxRetries;
            _subject.RetryWhenResult = retryWhenResult;
            _subject.RetryWhenException = retryWhenException;
            return this;
        }

        public AdvancedHttpRequestBuilder Retry(RetryStrategy retryStrategy)
        {
            _subject.MaxRetryCount = retryStrategy.MaxRetries;
            _subject.RetryWhenResult = retryStrategy.RetryWhenResult;
            _subject.RetryWhenException = retryStrategy.RetryWhenException;
            return this;
        }

        public AdvancedHttpRequestBuilder Cache(TimeSpan cachingTime)
        {
            _subject.CachingTime = cachingTime;
            return this;
        }

        public AdvancedHttpRequestBuilder TimeRateLime(TimeSpan maxInterval)
        {
            _subject.TimeRateLimit = maxInterval;
            return this;
        }

        public AdvancedHttpRequestBuilder WeightRateLimit(int weight, WeightRateLimitedFunctionConfiguration configuration)
        {
            _subject.WeightRateLimit = weight;
            _subject.WeightRateLimitConfiguration = configuration;
            return this;
        }

        public AdvancedHttpRequestBuilder UseHttpClient(HttpClient httpClient)
        {
            _subject.HttpClient = httpClient;
            return this;
        }

        public AdvancedHttpRequestBuilder WithUriBuilder(Func<object[], Task<string>> uriBuilder)
        {
            _subject.UriBuilder = uriBuilder;
            return this;
        }

        public AdvancedHttpRequest Build()
        {
            AsyncFunction<string, HttpResponseMessage> requestFunc = null;
            AsyncRetryFunction<string, HttpResponseMessage> retryFunc = null;
            AsyncCachedFunction<string, HttpResponseMessage> cachingFunc = null;
            AsyncTimeRateLimitedFunction<string, HttpResponseMessage> timeRatedFunc = null;
            AsyncWeightRateLimitedFunction<string, HttpResponseMessage> weightRatedFunc = null;

            FunctionBaseDecorator decorableFunc = requestFunc = new AsyncFunction<string, HttpResponseMessage>(async uri =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                return await _subject.HttpClient.SendAsync(request);
            });

            if (_subject.MaxRetryCount > 0)
            {
                decorableFunc = retryFunc = new AsyncRetryFunction<string, HttpResponseMessage>(decorableFunc, _subject.MaxRetryCount, _subject.RetryWhenResult, _subject.RetryWhenException);
            }
            if (_subject.CachingTime > TimeSpan.Zero)
            {
                decorableFunc = cachingFunc = new AsyncCachedFunction<string, HttpResponseMessage>(decorableFunc, _subject.CachingTime);
            }
            if (_subject.TimeRateLimit > TimeSpan.Zero)
            {
                decorableFunc = timeRatedFunc = new AsyncTimeRateLimitedFunction<string, HttpResponseMessage>(decorableFunc, _subject.TimeRateLimit, uri => Task.FromResult(cachingFunc?.HasCached(uri) ?? false));
            }
            if (_subject.WeightRateLimit > 0)
            {
                decorableFunc = weightRatedFunc = new AsyncWeightRateLimitedFunction<string, HttpResponseMessage>(decorableFunc, _subject.WeightRateLimit, _subject.WeightRateLimitConfiguration, uri => Task.FromResult(cachingFunc?.HasCached(uri) ?? false));
            }

            var func = new Func<string, Task<string>>(async uri =>
            {
                var result = (HttpResponseMessage)await decorableFunc.ExecuteInnerAsync(new object[] { uri });
                return await result.Content.ReadAsStringAsync();
            });

            return new AdvancedHttpRequest(
                _subject.HttpClient,
                requestFunc,
                retryFunc,
                cachingFunc,
                timeRatedFunc,
                weightRatedFunc,
                func,
                _subject.UriBuilder);
        }

        public class AdvancedHttpRequestSubject
        {
            public HttpClient HttpClient { get; set; }
            public int MaxRetryCount { get; set; }
            public TimeSpan CachingTime { get; set; }
            public TimeSpan TimeRateLimit { get; set; }
            public int WeightRateLimit { get; set; }
            public WeightRateLimitedFunctionConfiguration WeightRateLimitConfiguration { get; set; }
            public Func<object[], Task<string>> UriBuilder { get; set; }
            public Func<HttpResponseMessage, bool> RetryWhenResult { get; set; }
            public Func<Exception, bool> RetryWhenException { get; set; }
        }
    }
}