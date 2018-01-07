using System;
using System.Collections.Generic;
using System.Net.Http;

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

        public RestClientBuilder AddEndpoint(string name, AdvancedHttpRequest endpoint)
        {
            _subject.Endpoints.Add(name, endpoint);
            return this;
        }
        
        public RestClientBuilder AddEndpoint(string name, Action<AdvancedHttpRequestBuilder> buildFnc)
        {
            var builder = new AdvancedHttpRequestBuilder();
            builder.UseHttpClient(_subject.HttpClient);
            buildFnc(builder);
            _subject.Endpoints.Add(name, builder.Build());
            return this;
        }

        public RestClientBuilder WithHttpClient(HttpClient httpClient)
        {
            _subject.HttpClient = httpClient;
            return this;
        }

        public RestClient Build()
            => new RestClient(_subject.HttpClient, _subject.RateLimtBreakedStatusCode, _subject.DelayAfterRateLimitBreak, _subject.Endpoints);

        private class RestClientSubject
        {
            public HttpClient HttpClient { get; set; }
            public int RateLimtBreakedStatusCode { get; set; }
            public int DelayAfterRateLimitBreak { get; set; }
            public readonly Dictionary<string, AdvancedHttpRequest> Endpoints = new Dictionary<string, AdvancedHttpRequest>();
        }
    }
}