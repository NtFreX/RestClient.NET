﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NtFreX.RestClient.NET.Helper;

namespace NtFreX.RestClient.NET
{
    public sealed class RestClient : IDisposable
    {
        private readonly int? _breakedRequestLimitStatusCode;
        private readonly int? _delayAfterBreakedRequestLimit;
        private readonly Dictionary<string, AdvancedHttpRequest> _endpoints;
        
        public IndexerProperty<string, TimeSpan> MinInterval { get; }
        public IndexerProperty<string, TimeSpan> CachingTime { get; }

        public HttpClient HttpClient { get; }

        public event EventHandler RateLimitRaised;

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
                Task.Delay(_delayAfterBreakedRequestLimit ?? 0).GetAwaiter().GetResult();
            }
        }

        public async Task<string> CallEndpointAsync(string name, params object[] arguments)
            => await _endpoints[name].ExecuteAsync(arguments);
        public async Task<TimeSpan> IsRateLimitedAsync(string name, params object[] arguments)
            => await _endpoints[name].IsRateLimitedAsync(arguments);
        public bool IsCached(string name, params object[] arguments)
            => _endpoints[name].IsCached(arguments);

        public void Dispose()
        {
            HttpClient?.Dispose();
        }
    }
}
