using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NtFreX.RestClient.NET.Helper;
using System.Net.Http;

namespace NtFreX.RestClient.NET
{
    public sealed class RestClient
    {
        private readonly int? _breakedRequestLimitStatusCode;
        private readonly int? _delayAfterBreakedRequestLimit;
        private readonly Dictionary<string, AdvancedHttpRequest> _endpoints;

        private DateTime _blockedUntil = DateTime.MinValue;

        public IndexerProperty<string, TimeSpan> MinInterval { get; }
        public IndexerProperty<string, TimeSpan> CachingTime { get; }

        public HttpClient HttpClient { get; }

        public event EventHandler RateLimitRaised;
        public event EventHandler<(string EndpointName, object[] Arguments, string Result)> AfterEndpointCalled; 

        public RestClient(HttpClient httpClient, int breakedRequestLimitStatusCode, int delayAfterBreakedRequestLimit, Dictionary<string, AdvancedHttpRequest> endpoints)
        {
            _breakedRequestLimitStatusCode = breakedRequestLimitStatusCode;
            _delayAfterBreakedRequestLimit = delayAfterBreakedRequestLimit;
            _endpoints = new Dictionary<string, AdvancedHttpRequest>();

            MinInterval = new IndexerProperty<string, TimeSpan>(arg => _endpoints[arg].MinInterval);
            CachingTime = new IndexerProperty<string, TimeSpan>(arg => _endpoints[arg].CachingTime);
            HttpClient = httpClient;

            _endpoints = endpoints;

            endpoints.ToList().ForEach(x => x.Value.AfterRequestExecution += AfterRequestExecution);
        }

        private void AfterRequestExecution(object sender, HttpResponseMessage httpResponseMessage)
        {
            if ((int)httpResponseMessage.StatusCode == _breakedRequestLimitStatusCode)
            {
                RateLimitRaised?.Invoke(this, EventArgs.Empty);
                _blockedUntil = DateTime.Now + TimeSpan.FromMilliseconds(_delayAfterBreakedRequestLimit ?? 0);
            }
        }

        public async Task<string> CallEndpointAsync(string name, params object[] arguments)
        {
            if (_blockedUntil > DateTime.Now)
                await Task.Delay(_blockedUntil - DateTime.Now);

            var result = await _endpoints[name].ExecuteAsync(arguments);
            AfterEndpointCalled?.Invoke(this, (name, arguments, result));
            return result;
        }
        public async Task<TimeSpan> IsRateLimitedAsync(string name, params object[] arguments)
            => await _endpoints[name].IsRateLimitedAsync(arguments);
        public bool IsCached(string name, params object[] arguments)
            => _endpoints[name].IsCached(arguments);
    }
}
