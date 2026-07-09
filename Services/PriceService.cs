using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Finefolio.ValuationApi.Services.PriceProviders;

namespace Finefolio.ValuationApi.Services
{
    public class PriceService
    {
        private readonly IMoexPriceProvider _moexProvider;
        private readonly ITradingViewPriceProvider _tradingViewProvider;

        public PriceService(IMoexPriceProvider moexProvider, ITradingViewPriceProvider tradingViewProvider)
        {
            _moexProvider = moexProvider;
            _tradingViewProvider = tradingViewProvider;
        }

        public async Task<(decimal Price, string Provider, bool FromCache)> GetPriceAsync(string exchange, string ticker)
        {
            var isMoex = string.Equals(exchange, "MOEX", StringComparison.OrdinalIgnoreCase);
            try
            {
                if (isMoex)
                {
                    var res = await _moexProvider.GetPriceAsync(exchange, ticker);
                    if (res > 0) return (res, nameof(MoexPriceProvider), false);
                }
                else
                {
                    var res = await _tradingViewProvider.GetPriceAsync(exchange, ticker);
                    if (res > 0) return (res, nameof(TradingViewPriceProvider), false);
                }
            }
            catch
            {
                // provider failure is surfaced as no price
            }

            return (0m, "none", true);
        }
    }
}
