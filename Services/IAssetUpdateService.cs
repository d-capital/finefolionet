using System.Threading;
using System.Threading.Tasks;

namespace Finefolio.ValuationApi.Services;

public interface IAssetUpdateService
{
    Task UpdatePricesAsync(CancellationToken cancellationToken);
    Task UpdatePricesForExchangeAsync(string exchange, CancellationToken cancellationToken);
}
