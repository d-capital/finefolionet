using Finefolio.ValuationApi.Models;

namespace Finefolio.ValuationApi.Repositories;

public interface IValuationRepository
{
    Task<AssetDto?> GetAssetDataAsync(string exchange, string ticker, string lang);
    Task<IList<NetProfitHistoryDto>> GetNetIncomeHistoryAsync(int assetId);
    Task<IList<AssetLabelDto>> GetAssetLabelsAsync(int assetId);
    Task<IList<AssetDto>> GetAssetsByExchangeAsync(string exchange);
    Task UpdateAssetPriceAsync(int assetId, decimal close, DateTime closeLastUpdated);
    Task UpdateAssetMetadataAsync(
        int assetId,
        string? description,
        string? country,
        decimal? close,
        DateTime? closeLastUpdated,
        decimal? marketCapBasic,
        long? totalSharesOutstandingFundamental,
        decimal? earningsPerShareBasicTtm,
        decimal? beta,
        decimal? priceEarningsTtm,
        decimal? dividendsYield,
        decimal? freeCashFlowFy,
        decimal? debtToEquity,
        decimal? interestRateOnDebt,
        decimal? netDebt,
        decimal? equity,
        decimal? debt);
    Task UpsertNetIncomeAsync(int assetId, int year, double value);
}
