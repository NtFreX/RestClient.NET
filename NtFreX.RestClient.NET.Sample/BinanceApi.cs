﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NtFreX.RestClient.NET.Extensions;
using NtFreX.RestClient.NET.Flow;

namespace NtFreX.RestClient.NET.Sample
{
    public class BinanceApi : IDisposable
    {
        private readonly string _binanceApiKeySecret;

        public RestClient RestClient { get; }
        public WeightRateLimitedFunctionConfiguration RequestWeightRateLimitConfig { get; } = new WeightRateLimitedFunctionConfiguration(1200);

        public event EventHandler RateLimitRaised;

        public BinanceApi(string binanceApiKey, string binanceApiKeySecret)
        {
            _binanceApiKeySecret = binanceApiKeySecret;

            int[] statusCodesToRetry = { 500, 520 };
            var retryStrategy = new RetryStrategy(
                maxTries: 3,
                retryWhenResult: message => statusCodesToRetry.Contains((int)message.StatusCode),
                retryWhenException: exception => true);
            var signatureParameterFunc = new Func<object[], Uri, (string Name, string Value)>(GetSignatureQueryStringParam);

            RestClient = new RestClientBuilder()
                .WithHttpClient(new HttpClient())
                .HandleRateLimit(IsRateLimitRaised, 5000)
                .AddEndpoint(BinanceApiEndpointNames.Ping, builder => builder
                    .BaseUri("https://www.binance.com/api/v1/ping")
                    .WeightRateLimit(1, RequestWeightRateLimitConfig)
                    .Retry(retryStrategy))
                .AddEndpoint(BinanceApiEndpointNames.Time, builder => builder
                    .BaseUri("https://www.binance.com/api/v1/time")
                    .WeightRateLimit(1, RequestWeightRateLimitConfig)
                    .Retry(retryStrategy))
                .AddEndpoint(BinanceApiEndpointNames.ExchangeInfo, builder => builder
                    .BaseUri("https://www.binance.com/api/v1/exchangeInfo")
                    .WeightRateLimit(1, RequestWeightRateLimitConfig)
                    .Cache(TimeSpan.FromHours(1))
                    .Retry(retryStrategy)
                    .AfterExecution(UpdateRateLimitsAsync))
                .AddEndpoint(BinanceApiEndpointNames.Trades, builder => builder
                    .BaseUri("https://www.binance.com/api/v1/trades")
                    .AddQueryStringParam((arguments, uri) => ("symbol", arguments[0].ToString()))
                    .WeightRateLimit(1, RequestWeightRateLimitConfig)
                    .Retry(retryStrategy))
                .AddEndpoint(BinanceApiEndpointNames.HistoricalTrades, builder => builder
                    .BaseUri("https://www.binance.com/api/v1/historicalTrades")
                    .AddQueryStringParam((arguments, uri) => ("symbol", arguments[0].ToString()))
                    .AddQueryStringParam((arguments, uri) => ("limit", arguments[1].ToString()))
                    .AddQueryStringParam((arguments, uri) => ("fromId", arguments[2].ToString()))
                    .WeightRateLimit(100, RequestWeightRateLimitConfig)
                    .Cache(TimeSpan.MaxValue)
                    .Retry(retryStrategy))
                .AddEndpoint(BinanceApiEndpointNames.AggregatedTrades, builder => builder
                    .BaseUri("https://www.binance.com/api/v1/aggTrades")
                    .AddQueryStringParam((arguments, uri) => ("symbol", arguments[0].ToString()))
                    .AddQueryStringParam((arguments, uri) => ("startTime", ((DateTime)arguments[1]).ToUnixTimeMilliseconds().ToString()))
                    .AddQueryStringParam((arguments, uri) => ("endTime", ((DateTime)arguments[2]).ToUnixTimeMilliseconds().ToString()))
                    .WeightRateLimit(1, RequestWeightRateLimitConfig)
                    .Cache(TimeSpan.MaxValue)
                    .Retry(retryStrategy))
                .AddEndpoint(BinanceApiEndpointNames.Account, builder => builder
                    .BaseUri("https://www.binance.com/api/v3/account")
                    .AddQueryStringParam((arguments, uri) => ("timestamp", DateTime.UtcNow.AddSeconds(-1).ToUnixTimeMilliseconds().ToString()))
                    .AddQueryStringParam(signatureParameterFunc)
                    .AddHeader(() => ("X-MBX-APIKEY", binanceApiKey))
                    .WeightRateLimit(1, RequestWeightRateLimitConfig)
                    .Retry(retryStrategy))
                .AddEndpoint(BinanceApiEndpointNames.MyTrades, builder => builder
                    .BaseUri("https://www.binance.com/api/v3/myTrades")
                    .AddQueryStringParam((arguments, uri) => ("symbol", arguments[0].ToString()))
                    .AddQueryStringParam((arguments, uri) => ("timestamp", DateTime.UtcNow.AddSeconds(-1).ToUnixTimeMilliseconds().ToString()))
                    .AddQueryStringParam(signatureParameterFunc)
                    .AddHeader(() => ("X-MBX-APIKEY", binanceApiKey))
                    .WeightRateLimit(1, RequestWeightRateLimitConfig)
                    .Retry(retryStrategy))
                .Build();

            RestClient.RateLimitRaised += (sender, args) => RateLimitRaised?.Invoke(sender, args);
        }

        private async Task<bool> IsRateLimitRaised(HttpResponseMessage msg)
        {
            try
            {
                if (msg.StatusCode != HttpStatusCode.BadRequest)
                    return false;

                var conent = await msg.Content.ReadAsStringAsync();
                var obj = JObject.Parse(conent);
                return obj.Value<int>("code") == -1003;
            }
            catch
            {
                return false;
            }
        }
        private (string Name, string Value) GetSignatureQueryStringParam(object[] args, Uri uri)
        {
            var encryptor = new HMACSHA256(Encoding.UTF8.GetBytes(_binanceApiKeySecret));
            var signature = ByteToString(encryptor.ComputeHash(Encoding.UTF8.GetBytes(uri.Query.Replace("?", ""))));
            return ("signature", signature);
        }
        private async Task UpdateRateLimitsAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                return;

            var content = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(content);
            var rateLimits = obj.Value<JArray>("rateLimits");
            var requestRateLimit = rateLimits.First(x => x.Value<string>("rateLimitType") == "REQUESTS");
            RequestWeightRateLimitConfig.TotalWeightPerMinute = requestRateLimit.Value<int>("limit");
        }

        private string ByteToString(byte[] buff)
        {
            var value = "";
            foreach (var b in buff)
            {
                value += b.ToString("X2");
            }
            return value;
        }

        public static class BinanceApiEndpointNames
        {
            public static string Ping { get; } = nameof(Ping);
            public static string Time { get; } = nameof(Time);
            public static string ExchangeInfo { get; } = nameof(ExchangeInfo);
            public static string Trades { get; } = nameof(Trades);
            public static string HistoricalTrades { get; } = nameof(HistoricalTrades);
            public static string AggregatedTrades { get; } = nameof(AggregatedTrades);
            public static string Account { get; } = nameof(Account);
            public static string MyTrades { get; } = nameof(MyTrades);
        }

        public void Dispose()
        {
            RestClient?.HttpClient?.Dispose();
        }
    }
}