using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET
{
    public abstract class RestClient
    {
        private readonly int _breakedRequestLimitStatusCode;
        private readonly int _delayAfterBreakedRequestLimit;
        private readonly Dictionary<string, AdvancedHttpRequest<object[]>> _endpoints;

        public HttpClient HttpClient { get; }

        protected RestClient(int breakedRequestLimitStatusCode, int delayAfterBreakedRequestLimit, (string Name, Func<object[], string> UriBuilder, TimeSpan MaxInterval, TimeSpan CachingTime, int Retries, int[] StatusCodesToRetry)[] endpoints)
        {
            _breakedRequestLimitStatusCode = breakedRequestLimitStatusCode;
            _delayAfterBreakedRequestLimit = delayAfterBreakedRequestLimit;
            _endpoints = new Dictionary<string, AdvancedHttpRequest<object[]>>();

            HttpClient = new HttpClient();

            foreach (var endpoint in endpoints)
            {
                var request = new AdvancedHttpRequest<object[]>(
                    HttpClient, endpoint.UriBuilder, endpoint.MaxInterval,
                    endpoint.CachingTime, endpoint.Retries,
                    endpoint.StatusCodesToRetry.Concat(new[] { breakedRequestLimitStatusCode }).ToArray())
                {
                    BeforeResponseHandeled = BeforeResponseHandeledAsync
                };
                _endpoints.Add(endpoint.Name, request);
            }
        }

        private async Task BeforeResponseHandeledAsync(HttpResponseMessage httpResponseMessage)
        {
            if ((int)httpResponseMessage.StatusCode == _breakedRequestLimitStatusCode)
            {
                await Task.Delay(_delayAfterBreakedRequestLimit);
            }
        }

        protected async Task<string> CallEndpointAsync(string name, params object[] arguments)
            => await _endpoints[name].ExecuteAsync(arguments);
        protected TimeSpan GetMaxInterval(string name)
            => _endpoints[name].MaxInterval;
        protected bool IsCached(string name, params object[] arguments)
            => _endpoints[name].IsCached(arguments);
    }
}
