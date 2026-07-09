using Finefolio.ValuationApi.Models;
using Finefolio.ValuationApi.Repositories;

namespace Finefolio.ValuationApi.Services;

public class AssetFundamentalsService : IAssetFundamentalsService
{
    private readonly IAssetFundamentalsRepository _repository;

    public AssetFundamentalsService(IAssetFundamentalsRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> UpdateFundamentalsAsync(string exchange, string ticker, AssetFundamentalsUpdateDto request)
    {
        if (request == null || !request.HasAnyValue)
        {
            return false;
        }

        var asset = await _repository.GetAssetDataAsync(exchange, ticker, "en");
        if (asset == null)
        {
            return false;
        }

        return await _repository.UpdateFundamentalsAsync(asset.Id, request);
    }
}
