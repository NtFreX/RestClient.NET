using System.Collections.Generic;
using System.Linq;

namespace NtFreX.RestClient.NET
{
    public class RestClientBuilder
    {
        private readonly RestClientSubject _subject = new RestClientSubject();

        public RestClientBuilder HandleRateLimitStatusCode(int statusCode, int delayAfterLimitBreak)
        {
            _subject.RateLimtBreakedStatusCode = statusCode;
            _subject.DelayAfterRateLimitBreak = delayAfterLimitBreak;
            return this;
        }

        public RestClientBuilder AddEndpoint(EndpointBuilder.EndpointSubject definition)
        {
            _subject.Endpoints.Add(definition);
            return this;
        }

        public RestClient Build()
        {
            var endpoints = _subject.Endpoints.Select(x => (Name: x.Name, UriBuilder: x.UriBuilder, MaxInterval: x.MaxInterval, CacheTime: x.CacheTime, Retries: x.Retries, StatusCodesToRetry: x.StatusCodesToRetry.ToArray()));
            return new RestClient(_subject.RateLimtBreakedStatusCode, _subject.DelayAfterRateLimitBreak, endpoints.ToArray());
        }

        private class RestClientSubject
        {
            public int? RateLimtBreakedStatusCode { get; set; }
            public int? DelayAfterRateLimitBreak { get; set; }
            public List<EndpointBuilder.EndpointSubject> Endpoints = new List<EndpointBuilder.EndpointSubject>();
        }
    }
}