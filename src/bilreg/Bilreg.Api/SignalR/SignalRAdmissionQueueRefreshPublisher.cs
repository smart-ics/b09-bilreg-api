using Bilreg.Application.AdmisiContext.AntrianFeature;
using Microsoft.AspNetCore.SignalR;

namespace Bilreg.Api.SignalR;

public sealed class SignalRAdmissionQueueRefreshPublisher(
    IHubContext<AdmissionQueueRefreshHub> hubContext,
    ILogger<SignalRAdmissionQueueRefreshPublisher> logger) : IAdmissionQueueRefreshPublisher
{
    public async Task PublishAsync(string? loketKey, CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Clients.All.SendAsync(
                AdmissionQueueRefreshContracts.RefreshHintEvent,
                new AdmissionQueueRefreshHint(loketKey),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Admission queue SignalR refresh hint failed for LoketKey {LoketKey}; queue write truth is unchanged",
                loketKey);
        }
    }
}
