using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;
using NtFreX.RestClient.NET.Flow;

namespace NtFreX.RestClient.NET
{
    public class AdvancedHttpRequest
    {
        private readonly AsyncFunction<string, HttpResponseMessage> _requestFunc;
        private readonly AsyncCachedFunction<string, HttpResponseMessage> _cachingFunc;
        private readonly AsyncTimeRateLimitedFunction<string, HttpResponseMessage> _timeRatedFunc;
        private readonly AsyncWeightRateLimitedFunction<string, HttpResponseMessage> _weightRatedFunc;

        private readonly Func<string, Task<string>> _func;
        private readonly List<Func<object[], Uri, Task<(string Name, string Value)>>> _queryStringParameterResolvers;
        private readonly Func<object[], string> _baseUriBuilder;
        
        public HttpClient HttpClient { get; }
        public event EventHandler<HttpResponseMessage> AfterRequestExecution;
        public event EventHandler<object[]> BeforeRequestExecution; 

        public TimeSpan MinInterval => _timeRatedFunc?.MinInterval ?? TimeSpan.Zero;
        public TimeSpan CachingTime => _cachingFunc?.CachingTime ?? TimeSpan.Zero;

        public AdvancedHttpRequest(HttpClient httpClient, 
            AsyncFunction<string, HttpResponseMessage> requestFunc,
            AsyncRetryFunction<string, HttpResponseMessage> retryFunc,
            AsyncCachedFunction<string, HttpResponseMessage> cachingFunc,
            AsyncTimeRateLimitedFunction<string, HttpResponseMessage> timeRatedFunc,
            AsyncWeightRateLimitedFunction<string, HttpResponseMessage> weightRatedFunc,
            Func<string, Task<string>> func,
            List<Func<object[], Uri, Task<(string Name, string Value)>>> queryStringParameterResolvers,
            Func<object[], string> baseUriBuilder)
        {
            _requestFunc = requestFunc;
            _cachingFunc = cachingFunc;
            _timeRatedFunc = timeRatedFunc;
            _weightRatedFunc = weightRatedFunc;
            _func = func;
            _queryStringParameterResolvers = queryStringParameterResolvers;
            _baseUriBuilder = baseUriBuilder;

            retryFunc.AfterExecution += (sender, result) => AfterRequestExecution?.Invoke(this, (HttpResponseMessage) result);
            retryFunc.BeforeExecution += (sender, objects) => BeforeRequestExecution?.Invoke(this, objects);

            HttpClient = httpClient;
        }

        public async Task<string> ExecuteAsync(params object[] arguments)
            => await _func(await BuildUriAsync(arguments));
        public bool IsCached(params object[] arguments)
            => _cachingFunc?.HasCached((string) arguments[0]) ?? false;
        public async Task<TimeSpan> IsRateLimitedAsync(params object[] arguments)
        {
            if (_weightRatedFunc != null)
            {
                return await _weightRatedFunc.GetTimeToNextExecutionAsync((string) arguments[0]);
            }

            if (_timeRatedFunc != null)
            {
                return await _timeRatedFunc.GetTimeToNextExecutionAsync((string)arguments[0]);
            }

            return TimeSpan.Zero;
        }

        private async Task<string> BuildUriAsync(object[] arguments)
        {
            var currentUri = _baseUriBuilder(arguments);
            foreach (var queryStringParameterResolver in _queryStringParameterResolvers)
            {
                var param = await queryStringParameterResolver(arguments, new Uri(currentUri));
                currentUri = QueryHelpers.AddQueryString(currentUri, param.Name, param.Value);
            }
            return currentUri;
        }
    }
}
