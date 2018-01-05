using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET
{
    public abstract class AdvancedHttpRequestBase : IDisposable
    {
        private readonly int _maxRetries;
        private readonly int[] _replayOnStatusCode;
        private readonly HttpClient _httpClient;
        private readonly AsyncRateLimitedCachedFunction<string, string> _function;

        public Func<HttpResponseMessage, Task> BeforeResponseHandeled { get; set; }

        public TimeSpan MaxInterval => _function.MaxInterval;
        public TimeSpan CachingTime => _function.CachingTime;

        protected AdvancedHttpRequestBase(HttpClient httpClient, TimeSpan maxInterval, TimeSpan cachingTime, int maxRetries, params int[] replayOnStatusCode)
        {
            _maxRetries = maxRetries;
            _replayOnStatusCode = replayOnStatusCode;
            _httpClient = httpClient;
            _function = new AsyncRateLimitedCachedFunction<string, string>(maxInterval, cachingTime, GetStringAsyncFunction, IgnoreRateLimitAsyncFunction);
        }

        protected async Task<string> ExecuteInnerAsync(string uri)
            => await _function.ExecuteAsync(uri);
        protected async Task<TimeSpan> IsRateLimitedInnerAsync(string uri)
            => await _function.IsRateLimitedAsync(uri);
        protected bool IsCachedInner(string uri)
            => _function.IsCached(uri);

        private async Task<bool> IgnoreRateLimitAsyncFunction(string uri)
            => await Task.FromResult(_function.IsCached(uri));
        private async Task<string> GetStringAsyncFunction(string uri)
            => await GetStringInnerAsync(uri);

        private async Task<string> GetStringInnerAsync(string uri, int tryCounter = 0)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await _httpClient.SendAsync(request);

            if (BeforeResponseHandeled != null)
            {
                await BeforeResponseHandeled.Invoke(response);
            }

            if (_replayOnStatusCode.Any(x => x == (int)response.StatusCode) && tryCounter < _maxRetries)
            {
                return await GetStringInnerAsync(uri, tryCounter + 1);
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class AdvancedHttpRequest : AdvancedHttpRequestBase
    {
        private readonly Func<string> _uriBuilder;

        public AdvancedHttpRequest(HttpClient httpClient, Func<string> uriBuilder, TimeSpan maxInterval, TimeSpan cachingTime, int maxRetries, params int[] replayOnStatusCode)
            : base(httpClient, maxInterval, cachingTime, maxRetries, replayOnStatusCode)
        {
            _uriBuilder = uriBuilder;
        }

        public async Task<string> ExecuteAsync()
            => await ExecuteInnerAsync(_uriBuilder());
        public async Task<TimeSpan> IsRateLimitedAsync()
            => await IsRateLimitedInnerAsync(_uriBuilder());
        public bool IsCached()
            => IsCachedInner(_uriBuilder());
    }
    public class AdvancedHttpRequest<TArg1> : AdvancedHttpRequestBase
    {
        private readonly Func<TArg1, string> _uriBuilder;

        public AdvancedHttpRequest(HttpClient httpClient, Func<TArg1, string> uriBuilder, TimeSpan maxInterval, TimeSpan cachingTime, int maxRetries, params int[] replayOnStatusCode)
            : base(httpClient, maxInterval, cachingTime, maxRetries, replayOnStatusCode)
        {
            _uriBuilder = uriBuilder;
        }

        public async Task<string> ExecuteAsync(TArg1 arg1)
            => await ExecuteInnerAsync(_uriBuilder(arg1));
        public async Task<TimeSpan> IsRateLimitedAsync(TArg1 arg1)
            => await IsRateLimitedInnerAsync(_uriBuilder(arg1));
        public bool IsCached(TArg1 arg1)
            => IsCachedInner(_uriBuilder(arg1));
    }
    public class AdvancedHttpRequest<TArg1, TArg2> : AdvancedHttpRequestBase
    {
        private readonly Func<TArg1, TArg2, string> _uriBuilder;

        public AdvancedHttpRequest(HttpClient httpClient, Func<TArg1, TArg2, string> uriBuilder, TimeSpan maxInterval, TimeSpan cachingTime, int maxRetries, params int[] replayOnStatusCode)
            : base(httpClient, maxInterval, cachingTime, maxRetries, replayOnStatusCode)
        {
            _uriBuilder = uriBuilder;
        }

        public async Task<string> ExecuteAsync(TArg1 arg1, TArg2 arg2)
            => await ExecuteInnerAsync(_uriBuilder(arg1, arg2));
        public async Task<TimeSpan> IsRateLimitedAsync(TArg1 arg1, TArg2 arg2)
            => await IsRateLimitedInnerAsync(_uriBuilder(arg1, arg2));
        public bool IsCached(TArg1 arg1, TArg2 arg2)
            => IsCachedInner(_uriBuilder(arg1, arg2));
    }
    public class AdvancedHttpRequest<TArg1, TArg2, TArg3> : AdvancedHttpRequestBase
    {
        private readonly Func<TArg1, TArg2, TArg3, string> _uriBuilder;

        public AdvancedHttpRequest(HttpClient httpClient, Func<TArg1, TArg2, TArg3, string> uriBuilder, TimeSpan maxInterval, TimeSpan cachingTime, int maxRetries, params int[] replayOnStatusCode)
            : base(httpClient, maxInterval, cachingTime, maxRetries, replayOnStatusCode)
        {
            _uriBuilder = uriBuilder;
        }

        public async Task<string> ExecuteAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => await ExecuteInnerAsync(_uriBuilder(arg1, arg2, arg3));
        public async Task<TimeSpan> IsRateLimitedAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => await IsRateLimitedInnerAsync(_uriBuilder(arg1, arg2, arg3));
        public bool IsCached(TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => IsCachedInner(_uriBuilder(arg1, arg2, arg3));
    }
}
