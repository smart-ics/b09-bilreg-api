using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Infrastructure.LabContext.LabOwareFeature;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bilreg.Api.HostedServices;

public class LabOwareOutboundWorker : BackgroundService
{
    public const int DefaultBatchSize = 20;
    public const int DefaultIntervalSeconds = 10;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OwareOptions> _options;
    private readonly ILogger<LabOwareOutboundWorker> _logger;

    public LabOwareOutboundWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OwareOptions> options,
        ILogger<LabOwareOutboundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public virtual LabOwareProcessBatchResult? ProcessBatchOnce()
    {
        if (!_options.Value.WorkerEnabled)
            return null;

        try
        {
            var opt = _options.Value;
            var maxRetry = opt.MaxRetryCount > 0 ? opt.MaxRetryCount : 3;
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<LabOwareQueueProcessor>();
            return processor.ProcessBatch(
                batchSize: DefaultBatchSize,
                actorUserId: LabOwareQueueProcessor.WorkerUserId,
                maxRetryCount: maxRetry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LabOwareOutboundWorker encountered an error while processing queue batch.");
            return null;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LabOwareOutboundWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var opt = _options.Value;
            var intervalSec = opt.IntervalSeconds > 0 ? opt.IntervalSeconds : DefaultIntervalSeconds;

            if (opt.WorkerEnabled)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<LabOwareQueueProcessor>();
                    var maxRetry = opt.MaxRetryCount > 0 ? opt.MaxRetryCount : 3;
                    var result = processor.ProcessBatch(
                        batchSize: DefaultBatchSize,
                        actorUserId: LabOwareQueueProcessor.WorkerUserId,
                        maxRetryCount: maxRetry);

                    if (result.ProcessedCount > 0)
                    {
                        _logger.LogInformation(
                            "LabOwareOutboundWorker processed {ProcessedCount} items (Succeeded: {SucceededCount}, Failed: {FailedCount}).",
                            result.ProcessedCount, result.SucceededCount, result.FailedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LabOwareOutboundWorker encountered an error while processing queue batch.");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSec), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("LabOwareOutboundWorker stopped.");
    }
}
