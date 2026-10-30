using Bilreg.Application.AdmisiContext.AntrianFeature;
using Microsoft.AspNetCore.SignalR;

namespace Bilreg.Api.SignalR;

public sealed class SignalRClinicQueueRefreshPublisher(
    IHubContext<AdmissionQueueRefreshHub> hubContext,
    ILogger<SignalRClinicQueueRefreshPublisher> logger) : IClinicQueueRefreshPublisher
{
    public async Task PublishRefreshHint(string poliId, string dokterId, CancellationToken cancellationToken = default)
    {
        try
        {
            await hubContext.Clients.All.SendAsync(
                ClinicQueueRefreshContracts.RefreshHintEvent,
                new ClinicQueueRefreshHint(poliId, dokterId),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Clinic queue SignalR refresh hint failed for PoliId {PoliId}, DokterId {DokterId}; queue write truth is unchanged",
                poliId,
                dokterId);
        }
    }
}
