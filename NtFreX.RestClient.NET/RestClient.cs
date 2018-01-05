using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET
{
    public sealed class RestClient : IDisposable
    {
        private readonly int? _breakedRequestLimitStatusCode;
        private readonly int? _delayAfterBreakedRequestLimit;
        private readonly Dictionary<string, AdvancedHttpRequest<object[]>> _endpoints;

        public HttpClient HttpClient { get; }

        public RestClient(int? breakedRequestLimitStatusCode, int? delayAfterBreakedRequestLimit, (string Name, Func<object[], string> UriBuilder, TimeSpan MaxInterval, TimeSpan CachingTime, int Retries, int[] StatusCodesToRetry)[] endpoints)
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
                    breakedRequestLimitStatusCode.HasValue ? endpoint.StatusCodesToRetry.Concat(new[] { breakedRequestLimitStatusCode.Value }).ToArray() : endpoint.StatusCodesToRetry)
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
                await Task.Delay(_delayAfterBreakedRequestLimit ?? 0);
            }
        }

        public async Task<string> CallEndpointAsync(string name, params object[] arguments)
            => await _endpoints[name].ExecuteAsync(arguments);
        public TimeSpan GetMaxInterval(string name)
            => _endpoints[name].MaxInterval;
        public bool IsCached(string name, params object[] arguments)
            => _endpoints[name].IsCached(arguments);

        public void Dispose()
        {
            HttpClient?.Dispose();
        }
    }
}
