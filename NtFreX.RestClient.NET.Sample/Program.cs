using System;
using System.Linq;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
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
            Console.ReadLine();
        }
    }
}
