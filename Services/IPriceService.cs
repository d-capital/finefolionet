using System.Threading.Tasks;

namespace Finefolio.ValuationApi.Services;

public interface IPriceService
{
    Task<decimal> GetPriceAsync(string exchange, string ticker);
}
