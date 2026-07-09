using Finefolio.ValuationApi.Models;

namespace Finefolio.ValuationApi.Repositories;

public interface IAssetFundamentalsRepository
{
    Task<AssetDto?> GetAssetDataAsync(string exchange, string ticker, string lang);
    Task<bool> UpdateFundamentalsAsync(int assetId, AssetFundamentalsUpdateDto request);
}
