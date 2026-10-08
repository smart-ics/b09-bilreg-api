using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bilreg.Infrastructure.AdmisiRanapContext.DigitalSignFeature;

public class GeneralConsentArchiveHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GeneralConsentArchiveHostedService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2);

    public GeneralConsentArchiveHostedService(
        IServiceProvider serviceProvider,
        ILogger<GeneralConsentArchiveHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GeneralConsentArchiveHostedService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var worker = scope.ServiceProvider.GetRequiredService<GeneralConsentArchiveWorker>();
                
                var result = await worker.ProcessBatchAsync(
                    batchSize: 20,
                    triggerType: "SCHEDULED_WORKER",
                    userId: "SYSTEM_WORKER",
                    cancellationToken: stoppingToken);

                if (result.ProcessedCount > 0)
                {
                    _logger.LogInformation(
                        "GeneralConsentArchiveHostedService processed batch: {Processed} items, {Succeeded} succeeded, {Failed} failed, {Skipped} skipped",
                        result.ProcessedCount, result.SucceededCount, result.FailedCount, result.SkippedCount);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during GeneralConsentArchiveHostedService execution cycle.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("GeneralConsentArchiveHostedService stopped.");
    }
}
