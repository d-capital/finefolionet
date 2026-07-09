using System.Collections.Generic;
using System.Threading.Tasks;
using Finefolio.ValuationApi.Models;

namespace Finefolio.ValuationApi.Services.PriceProviders;

public interface IPriceProvider
{
    Task<decimal> GetPriceAsync(string exchange, string ticker);
}

public interface IMoexPriceProvider : IPriceProvider
{
}

public interface ITradingViewPriceProvider : IPriceProvider
{
    Task<TradingViewQuote?> GetQuoteAsync(string exchange, string ticker);
    Task<IDictionary<string, TradingViewQuote?>> GetQuotesAsync(string exchange, IReadOnlyList<string> tickers);
}
