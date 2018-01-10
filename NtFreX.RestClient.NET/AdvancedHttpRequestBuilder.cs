using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
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
        public AdvancedHttpRequestBuilder HttpMethod(HttpMethod method)
        {
            _subject.HttpMethod = method;
            return this;
        }
        public AdvancedHttpRequestBuilder BaseUri(string baseUri)
        {
            _subject.BaseUriBuilder = args => baseUri;
            return this;
        }
        public AdvancedHttpRequestBuilder BaseUri(Func<object[], string> baseUriBuilder)
        {
            _subject.BaseUriBuilder = baseUriBuilder;
            return this;
        }
        public AdvancedHttpRequestBuilder AddHeader(Func<(string Name, string Value)> headerResolver)
        {
            _subject.HeaderResolvers.Add(headerResolver);
            return this;
        }
        public AdvancedHttpRequestBuilder AddQueryStringParam(Func<object[], Uri, (string Name, string Value)> paramResolver)
            => AddQueryStringParam((args, uri) => Task.FromResult(paramResolver(args, uri)));
        public AdvancedHttpRequestBuilder AddQueryStringParam(Func<object[], Uri, Task<(string Name, string Value)>> paramResolver)
        {
            _subject.QueryStringParameterResolvers.Add(paramResolver);
            return this;
        }
        public AdvancedHttpRequestBuilder AfterExecution(Func<HttpResponseMessage, Task> afterExecution)
        {
            _subject.AfterExecution = afterExecution;
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
                var request = new HttpRequestMessage(_subject.HttpMethod, uri);
                foreach (var headerResolver in _subject.HeaderResolvers)
                {
                    var header = headerResolver();
                    request.Headers.Add(header.Name, header.Value);   
                }
                return await _subject.HttpClient.SendAsync(request);
            });

            if (_subject.MaxRetryCount > 0)
            {
                decorableFunc = retryFunc = new AsyncRetryFunction<string, HttpResponseMessage>(decorableFunc, _subject.MaxRetryCount, _subject.RetryWhenResult, _subject.RetryWhenException);
            }
            if (_subject.TimeRateLimit > TimeSpan.Zero)
            {
                decorableFunc = timeRatedFunc = new AsyncTimeRateLimitedFunction<string, HttpResponseMessage>(decorableFunc, _subject.TimeRateLimit, uri => Task.FromResult(cachingFunc?.HasCached(uri) ?? false));
            }
            if (_subject.WeightRateLimit > 0)
            {
                decorableFunc = weightRatedFunc = new AsyncWeightRateLimitedFunction<string, HttpResponseMessage>(decorableFunc, _subject.WeightRateLimit, _subject.WeightRateLimitConfiguration, uri => Task.FromResult(cachingFunc?.HasCached(uri) ?? false));
            }
            if (_subject.CachingTime > TimeSpan.Zero)
            {
                decorableFunc = cachingFunc = new AsyncCachedFunction<string, HttpResponseMessage>(decorableFunc, _subject.CachingTime);
            }

            var func = new Func<string, Task<string>>(async uri =>
            {
                var result = (HttpResponseMessage)await decorableFunc.ExecuteInnerAsync(new object[] { uri });
                if(_subject.AfterExecution != null)
                    await _subject.AfterExecution.Invoke(result);
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
                _subject.QueryStringParameterResolvers,
                _subject.BaseUriBuilder);
        }

        public class AdvancedHttpRequestSubject
        {
            public Func<HttpResponseMessage, Task> AfterExecution { get; set; }
            public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;
            public Func<object[], string> BaseUriBuilder { get; set; }
            public HttpClient HttpClient { get; set; }
            public int MaxRetryCount { get; set; }
            public TimeSpan CachingTime { get; set; }
            public TimeSpan TimeRateLimit { get; set; }
            public int WeightRateLimit { get; set; }
            public WeightRateLimitedFunctionConfiguration WeightRateLimitConfiguration { get; set; }
            public List<Func<object[], Uri, Task<(string Name, string Value)>>> QueryStringParameterResolvers { get; set; } = new List<Func<object[], Uri, Task<(string Name, string Value)>>>();
            public List<Func<(string Name, string Value)>> HeaderResolvers { get; set; } = new List<Func<(string Name, string Value)>>();
            public Func<HttpResponseMessage, bool> RetryWhenResult { get; set; }
            public Func<Exception, bool> RetryWhenException { get; set; }
        }
    }
}