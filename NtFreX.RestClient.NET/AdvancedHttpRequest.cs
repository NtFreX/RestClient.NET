using System;
using System.Net.Http;
using System.Threading.Tasks;
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
        private readonly Func<object[], Task<string>> _uriBuilder;

        public HttpClient HttpClient { get; }
        public event EventHandler<HttpResponseMessage> AfterRequestExecution;

        public TimeSpan MinInterval => _timeRatedFunc?.MinInterval ?? TimeSpan.Zero;
        public TimeSpan CachingTime => _cachingFunc?.CachingTime ?? TimeSpan.Zero;

        public AdvancedHttpRequest(HttpClient httpClient, 
            AsyncFunction<string, HttpResponseMessage> requestFunc,
            AsyncRetryFunction<string, HttpResponseMessage> retryFunc,
            AsyncCachedFunction<string, HttpResponseMessage> cachingFunc,
            AsyncTimeRateLimitedFunction<string, HttpResponseMessage> timeRatedFunc,
            AsyncWeightRateLimitedFunction<string, HttpResponseMessage> weightRatedFunc,
            Func<string, Task<string>> func,
            Func<object[], Task<string>> uriBuilder)
        {
            _requestFunc = requestFunc;
            _cachingFunc = cachingFunc;
            _timeRatedFunc = timeRatedFunc;
            _weightRatedFunc = weightRatedFunc;
            _func = func;
            _uriBuilder = uriBuilder;

            retryFunc.AfterExecution += (sender, result) => AfterRequestExecution?.Invoke(this, (HttpResponseMessage) result);

            HttpClient = httpClient;
        }

        public async Task<string> ExecuteAsync(params object[] arguments)
            => await _func(await _uriBuilder(arguments));
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
    }
}
