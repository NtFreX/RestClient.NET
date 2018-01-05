using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NtFreX.RestClient.NET.Sample
{
    public class BinanceApi : RestClient
    {
        private static readonly int[] StatusCodesToRetry = { 500, 520 };

        public BinanceApi()
            : base(419, 5000, new(string Name, Func<object[], string> UriBuilder, TimeSpan MaxInterval, TimeSpan CachingTime, int Retries, int[] StatusCodesToRetry)[]
            {
                (Name: BinanceApiEndpointNames.ExchangeInfo, UriBuilder: args => "https://www.binance.com/api/v1/exchangeInfo", MaxInterval: TimeSpan.FromSeconds(5), CachingTime: TimeSpan.FromDays(1), Retries: 3, StatusCodesToRetry: StatusCodesToRetry),
                (Name: BinanceApiEndpointNames.AggregatedTrades, UriBuilder: args => $"https://www.binance.com/api/v1/aggTrades?symbol={args[0]}&startTime={((DateTime)args[1]).ToUnixTimeMilliseconds()}&endTime={((DateTime)args[2]).ToUnixTimeMilliseconds()}", MaxInterval: TimeSpan.FromSeconds(3), CachingTime: TimeSpan.MaxValue, Retries: 3, StatusCodesToRetry: StatusCodesToRetry),
                (Name: BinanceApiEndpointNames.Trades, UriBuilder: args => $"https://www.binance.com/api/v1/trades?symbol={args[0]}", MaxInterval: TimeSpan.FromSeconds(3), CachingTime: TimeSpan.MaxValue, Retries: 3, StatusCodesToRetry: StatusCodesToRetry)
            })
        {
            GetExchangeSymbols = new AsyncCachedFunction<List<string>>(GetExchangeSymbolsAsync, TimeSpan.FromMinutes(10));
            GetExchangeRate = new AsyncRateLimitedFunction<string, string, DateTime, double>(GetExchangeRateAsync, GetMaxInterval(BinanceApiEndpointNames.AggregatedTrades), DoNotRateLimitGetExchangeRateAsync);

            _getSupportedSymbol = new AsyncCachedFunction<string, string, string>(GetSupportedSymbolAsync, TimeSpan.FromMinutes(10));
        }

        #region Public
        public readonly AsyncRateLimitedFunction<string, string, DateTime, double> GetExchangeRate;
        private async Task<double> GetExchangeRateAsync(string originCurrency, string targetCurrency, DateTime dateTime)
        {
            var startAndEnd = GetStartAndEndForGetExchangeRate(dateTime);
            var symbol = await _getSupportedSymbol.ExecuteAsync(originCurrency, targetCurrency);
            var searchInMiliseconds = dateTime.ToUnixTimeMilliseconds();

            var trades = await CallEndpointAsync(BinanceApiEndpointNames.AggregatedTrades, symbol, startAndEnd.Start, startAndEnd.End);
            var amount = GetNearestPrice(searchInMiliseconds, trades, items => items.Value<long>("T"), items => items.Value<double>("p"), false);
            if (amount != null)
                return amount.Value;

            //TODO: is this needed because no aggregated trades exists allready ??
            trades = await CallEndpointAsync(BinanceApiEndpointNames.Trades, symbol);
            amount = GetNearestPrice(searchInMiliseconds, trades, items => items.Value<long>("time"), items => items.Value<double>("price"), true);
            if (amount != null)
                return amount.Value;

            throw new Exception($"No exchange rate for `{symbol}` on `{dateTime}` found.");
        }
        private double? GetNearestPrice(long dateTimeInMiliseconds, string jsonArray, Func<JObject, long> dateTimeSelector, Func<JObject, double> priceSelector, bool searchAll)
        {
            var maxTradeOffset = 3600000;
            var lastTradeTime = 0L;
            var lastTradePrice = 0.0;
            var bestTradePrice = 0.0;
            var bestTradeDifference = long.MaxValue;
            foreach (var trade in JArray.Parse(jsonArray))
            {
                var items = trade as JObject;
                if (items == null)
                    continue;

                var tradeTime = dateTimeSelector(items);
                var tradePrice = priceSelector(items);

                if (searchAll)
                {
                    var tradeDifference = tradeTime - dateTimeInMiliseconds;
                    if (tradeDifference < bestTradeDifference)
                    {
                        bestTradeDifference = tradeDifference;
                        bestTradePrice = tradePrice;
                    }
                }
                else if (tradeTime > dateTimeInMiliseconds)
                {
                    var tradeDifference = tradeTime - dateTimeInMiliseconds;
                    var lastTradeDifference = (lastTradeTime - dateTimeInMiliseconds) * -1;
                    if (tradeDifference > lastTradeDifference)
                    {
                        if (lastTradeDifference >= maxTradeOffset)
                            throw new Exception($"The found exchange rate for is `{lastTradeDifference}` miliseconds older then the searched one.");
                        return lastTradePrice;
                    }

                    if (tradeDifference >= maxTradeOffset)
                        throw new Exception($"The found exchange rate for is `{tradeDifference}` miliseconds newer then the searched one.");
                    if (tradeDifference < lastTradeDifference)
                    {
                        return tradePrice;
                    }
                    return tradePrice;
                }

                lastTradeTime = tradeTime;
                lastTradePrice = tradePrice;
            }

            if (searchAll)
            {
                if (bestTradeDifference >= maxTradeOffset)
                    throw new Exception($"The found exchange rate for is `{bestTradeDifference}` miliseconds older then the searched one.");
                return bestTradePrice;
            }

            if (lastTradeTime == 0)
                return null;

            var lastTradeOffset = (lastTradeTime - dateTimeInMiliseconds) * -1;
            if (lastTradeOffset >= maxTradeOffset)
                throw new Exception($"The found exchange rate for is `{lastTradeOffset}` miliseconds older then the searched one.");

            return null;
        }
        private async Task<bool> DoNotRateLimitGetExchangeRateAsync(string originCurrency, string targetCurrency, DateTime dateTime)
        {
            var startAndEnd = GetStartAndEndForGetExchangeRate(dateTime);
            var symbol = await _getSupportedSymbol.ExecuteAsync(originCurrency, targetCurrency);
            return IsCached(BinanceApiEndpointNames.AggregatedTrades, symbol, startAndEnd.Start, startAndEnd.End);
        }
        private (DateTime Start, DateTime End) GetStartAndEndForGetExchangeRate(DateTime dateTime)
            => (dateTime.AddMinutes(-5), dateTime.AddMinutes(5));

        public readonly AsyncCachedFunction<List<string>> GetExchangeSymbols;
        private async Task<List<string>> GetExchangeSymbolsAsync()
        {
            var response = await CallEndpointAsync(BinanceApiEndpointNames.ExchangeInfo);
            var json = JObject.Parse(response);
            return json.Value<JArray>("symbols").Select(x =>
            {
                var obj = x as JObject;
                return obj?.Value<string>("symbol");
            }).ToList();
        }

        public async Task<bool> DoExchangeRatesExistAsync(string originCurrency, string targetCurrency)
            => !string.IsNullOrEmpty(await _getSupportedSymbol.ExecuteAsync(originCurrency, targetCurrency));
        #endregion

        #region Private
        private readonly AsyncCachedFunction<string, string, string> _getSupportedSymbol;
        private async Task<string> GetSupportedSymbolAsync(string originCurrency, string targetCurrency)
        {
            var exchangeRateSymbols = await GetExchangeSymbols.ExecuteAsync();
            exchangeRateSymbols = exchangeRateSymbols.Select(x => x.ToUpper()).ToList();
            if (exchangeRateSymbols.Contains(originCurrency + targetCurrency))
            {
                return originCurrency + targetCurrency;
            }
            if (exchangeRateSymbols.Contains(targetCurrency + originCurrency))
            {
                return targetCurrency + originCurrency;
            }
            return null;
        }
        #endregion
        
        private static class BinanceApiEndpointNames
        {
            public static string ExchangeInfo { get; } = nameof(ExchangeInfo);
            public static string AggregatedTrades { get; } = nameof(AggregatedTrades);
            public static string Trades { get; } = nameof(Trades);
        }
    }
}