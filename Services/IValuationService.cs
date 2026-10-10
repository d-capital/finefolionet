using Finefolio.ValuationApi.Models;

namespace Finefolio.ValuationApi.Services;

public interface IValuationService
{
    Task<IReadOnlyList<StockScreenerCandidateResponse>> GetScreenerCandidatesAsync(
        decimal minDividend,
        string? exchange,
        CancellationToken cancellationToken);
    Task<ValuationResultDto?> GetValuationAsync(string exchange, string ticker, string lang);
    Task<DcfResultDto?> GetDcfValuationAsync(string exchange, string ticker, string lang);
    Task<bool> AddOrUpdateNetIncomeAsync(string exchange, string ticker, int year, double value);
}
