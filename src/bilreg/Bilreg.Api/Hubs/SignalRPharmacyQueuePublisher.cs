using Bilreg.Application.ApotekContext.QueueFeature;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Bilreg.Api.Hubs;

/// <summary>
/// SignalR publisher adapter that broadcasts pharmacy queue domain events to client web applications and displays.
/// Implements hint-based notifications: sends lightweight hints and logs/swallows any transport exceptions
/// so database transactions and core queue state transitions are never aborted due to real-time messaging failures.
/// </summary>
public class SignalRPharmacyQueuePublisher(
    IHubContext<PharmacyQueueHub> hubContext,
    ILogger<SignalRPharmacyQueuePublisher> logger) : IPharmacyQueuePublisher
{
    public async Task PublishWorklistRefresh(string depoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hint = new PharmacyWorklistRefreshHint(depoId ?? string.Empty);
            if (string.IsNullOrWhiteSpace(depoId))
            {
                await hubContext.Clients.All.SendAsync(
                    PharmacyQueueContracts.NotifyWorklistRefreshEvent,
                    hint,
                    cancellationToken);
            }
            else
            {
                await hubContext.Clients.Group(PharmacyQueueContracts.DepoGroup(depoId)).SendAsync(
                    PharmacyQueueContracts.NotifyWorklistRefreshEvent,
                    hint,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Pharmacy queue SignalR worklist refresh hint failed for DepoId {DepoId}; queue state remains unchanged",
                depoId);
        }
    }

    public async Task PublishDisplayRefresh(string loketKey, long announcementVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var hint = new PharmacyDisplayRefreshHint(loketKey ?? string.Empty, announcementVersion);
            await hubContext.Clients.All.SendAsync(
                PharmacyQueueContracts.NotifyDisplayRefreshEvent,
                hint,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Pharmacy queue SignalR display refresh hint failed for LoketKey {LoketKey}, version {Version}; queue state remains unchanged",
                loketKey,
                announcementVersion);
        }
    }
}
