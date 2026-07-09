using Finefolio.ValuationApi.Models;

namespace Finefolio.ValuationApi.Services;

public interface IValuationService
{
    Task<ValuationResultDto?> GetValuationAsync(string exchange, string ticker, string lang);
    Task<bool> AddOrUpdateNetIncomeAsync(string exchange, string ticker, int year, double value);
}
