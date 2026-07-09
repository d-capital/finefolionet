using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Finefolio.ValuationApi.Models;
using Finefolio.ValuationApi.Repositories;
using Finefolio.ValuationApi.Services.PriceProviders;

namespace Finefolio.ValuationApi.Services;

public class AssetUpdateService : IAssetUpdateService
{
    private readonly IValuationRepository _repository;
    private readonly IMoexPriceProvider _moexProvider;
    private readonly ITradingViewPriceProvider _tradingViewProvider;

    public AssetUpdateService(
        IValuationRepository repository,
        IMoexPriceProvider moexProvider,
        ITradingViewPriceProvider tradingViewProvider)
    {
        _repository = repository;
        _moexProvider = moexProvider;
        _tradingViewProvider = tradingViewProvider;
    }

    public async Task UpdatePricesAsync(CancellationToken cancellationToken)
    {
        await UpdatePricesForExchangeAsync("NYSE", cancellationToken);
        await UpdatePricesForExchangeAsync("NASDAQ", cancellationToken);
        await UpdatePricesForExchangeAsync("MOEX", cancellationToken);
    }

    public async Task UpdatePricesForExchangeAsync(string exchange, CancellationToken cancellationToken)
    {
        if (exchange.Equals("MOEX", StringComparison.OrdinalIgnoreCase))
        {
            await UpdateMoexPricesAsync(cancellationToken);
        }
        else
        {
            await UpdateUsExchangeMetadataAsync(exchange, cancellationToken);
        }
    }

    private async Task UpdateMoexPricesAsync(CancellationToken cancellationToken)
    {
        var assets = await _repository.GetAssetsByExchangeAsync("MOEX");
        if (assets == null || assets.Count == 0)
        {
            return;
        }

        foreach (var asset in assets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(asset.Ticker) || asset.Id == 0)
            {
                continue;
            }

            try
            {
                var price = await _moexProvider.GetPriceAsync("MOEX", asset.Ticker);
                if (price > 0)
                {
                    await _repository.UpdateAssetPriceAsync(asset.Id, price, DateTime.UtcNow);
                }
            }
            catch
            {
                // continue updating remaining MOEX assets
            }
        }
    }

    private async Task UpdateUsExchangeMetadataAsync(string exchange, CancellationToken cancellationToken)
    {
        var assets = await _repository.GetAssetsByExchangeAsync(exchange);
        if (assets == null || assets.Count == 0)
        {
            return;
        }

        var tickers = assets
            .Where(a => !string.IsNullOrWhiteSpace(a.Ticker) && a.Id != 0)
            .Select(a => a.Ticker!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tickers.Count == 0)
        {
            return;
        }

        try
        {
            var quoteMap = await _tradingViewProvider.GetQuotesAsync(exchange, tickers);
            foreach (var asset in assets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (asset.Id == 0 || string.IsNullOrWhiteSpace(asset.Ticker))
                {
                    continue;
                }

                if (!quoteMap.TryGetValue(asset.Ticker.Trim(), out var quote) || quote?.Close <= 0)
                {
                    continue;
                }

                await _repository.UpdateAssetMetadataAsync(
                    asset.Id,
                    quote?.Description,
                    quote?.Country,
                    quote?.Close,
                    DateTime.UtcNow,
                    quote?.MarketCapBasic,
                    quote?.EarningsPerShareBasicTtm,
                    quote?.PriceEarningsTtm,
                    quote?.DividendsYield,
                    quote?.FreeCashFlowFy,
                    quote?.DebtToEquity);
            }
        }
        catch
        {
            // If batched TradingView scan fails, do not block the rest of the update cycle.
        }
    }
}
