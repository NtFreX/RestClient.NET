using System;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var binanceApi = new BinanceApi();
            var symbols = await binanceApi.GetExchangeSymbols.ExecuteAsync();
            foreach (var symbol in symbols)
            {
                var currencyOne = symbol.Substring(0, 3);
                var currencyTwo = symbol.Substring(3);
                var exchangeRate = await binanceApi.GetExchangeRate.ExecuteAsync(currencyOne, currencyTwo, DateTime.Now);
                Console.WriteLine($"{currencyOne} - {currencyTwo} = {exchangeRate}");
            }
            Console.ReadLine();
        }
    }
}
