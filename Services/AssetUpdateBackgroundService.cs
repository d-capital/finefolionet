using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Finefolio.ValuationApi.Services;

public class AssetUpdateBackgroundService : BackgroundService
{
    private readonly IAssetUpdateService _updateService;
    private readonly ILogger<AssetUpdateBackgroundService> _logger;

    public AssetUpdateBackgroundService(
        IAssetUpdateService updateService,
        ILogger<AssetUpdateBackgroundService> logger)
    {
        _updateService = updateService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Asset update background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRunUtc = GetNextRunUtc();
                var delay = nextRunUtc - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Waiting {Delay} until next update at {NextRunUtc} UTC.", delay, nextRunUtc);
                    await Task.Delay(delay, stoppingToken);
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                _logger.LogInformation("Starting scheduled asset price update at {TimeUtc} UTC.", DateTime.UtcNow);
                await _updateService.UpdatePricesAsync(stoppingToken);
                _logger.LogInformation("Scheduled asset price update completed at {TimeUtc} UTC.", DateTime.UtcNow);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Asset update background service failed.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Asset update background service stopping.");
    }

    private static DateTime GetNextRunUtc()
    {
        var now = DateTime.UtcNow;
        var moscow = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        var nowMoscow = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.Utc, moscow);

        var nextRunMoscow = new DateTime(nowMoscow.Year, nowMoscow.Month, nowMoscow.Day, 5, 0, 0, DateTimeKind.Unspecified);
        if (nowMoscow >= nextRunMoscow)
        {
            nextRunMoscow = nextRunMoscow.AddDays(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(nextRunMoscow, moscow);
    }
}
