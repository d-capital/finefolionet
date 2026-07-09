using Finefolio.ValuationApi.Models;

namespace Finefolio.ValuationApi.Services;

public interface IAssetFundamentalsService
{
    Task<bool> UpdateFundamentalsAsync(string exchange, string ticker, AssetFundamentalsUpdateDto request);
}
