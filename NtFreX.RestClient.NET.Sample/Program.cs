using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NtFreX.RestClient.NET.Flow;

namespace NtFreX.RestClient.NET.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var rateLimitConfiguration = new WeightRateLimitedFunctionConfiguration(250);

                //var cached = new AsyncCachedFunction<string>(async () => await Task.FromResult("Hello world"), TimeSpan.Zero);
                var weightRateLimited = new AsyncWeightRateLimitedFunction<string>(async () => await Task.FromResult("Hello world") /*cached*/, 5, rateLimitConfiguration/*, () => Task.FromResult(cached.HasCached())*/);
                var timeRateLimited = new AsyncTimeRateLimitedFunction<string>(weightRateLimited, TimeSpan.FromSeconds(1)/*, () => Task.FromResult(cached.HasCached())*/);
                
                var request = new AdvancedHttpRequestBuilder()
                    .UseHttpClient(new HttpClient())
                    .WithUriBuilder(_ => Task.FromResult("http://www.google.com"))
                    .Cache(TimeSpan.FromSeconds(5))
                    .TimeRateLime(TimeSpan.FromSeconds(1))
                    .WeightRateLimit(5, rateLimitConfiguration)
                    .Retry(3, message => !message.IsSuccessStatusCode, exception => true)
                    .Build();

                var googleContent = await request.ExecuteAsync();
                Console.WriteLine(googleContent);

                for (int i = 0; i < 100; i++)
                {
                    Console.WriteLine($"{DateTime.Now} - {await timeRateLimited.ExecuteAsync()}");
                }

                using (var binanceApi = new BinanceApi())
                {
                    binanceApi.RateLimitRaised += (sender, eventArgs) => Console.WriteLine("Rate limit raised!");

                    var symbols = await binanceApi.GetExchangeSymbols.ExecuteAsync();
                    await Task.WhenAll(symbols.Select(symbol => Task.Run(async () =>
                    {
                        var currencyOne = symbol.Substring(0, 3);
                        var currencyTwo = symbol.Substring(3);
                        var exchangeRate = await binanceApi.GetExchangeRate.ExecuteAsync(currencyOne, currencyTwo, DateTime.Now);
                        Console.WriteLine($"{DateTime.Now} : {currencyOne} - {currencyTwo} = {exchangeRate}");
                    })));

                }
            }
            catch
            {
                
            }
            Console.WriteLine("END");
            Console.ReadLine();
        }
    }
}
