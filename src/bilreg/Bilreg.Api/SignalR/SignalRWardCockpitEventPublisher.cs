using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Bilreg.Api.SignalR;

public sealed class SignalRWardCockpitEventPublisher : IWardCockpitEventPublisher
{
    private readonly IHubContext<WardCockpitHub> _hubContext;
    private readonly ILogger<SignalRWardCockpitEventPublisher> _logger;

    public SignalRWardCockpitEventPublisher(
        IHubContext<WardCockpitHub> hubContext,
        ILogger<SignalRWardCockpitEventPublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishBedStatusChangedAsync(
        string bangsalId,
        string bedId,
        string newStatus,
        object? currentOccupancy = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new BedStatusChangedPayload(bedId, newStatus, currentOccupancy);
        await PublishBedStatusChangedAsync(bangsalId, payload, cancellationToken);
    }

    public async Task PublishBedStatusChangedAsync(
        string bangsalId,
        BedStatusChangedPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bangsalId))
            return;

        try
        {
            var groupName = WardCockpitContracts.GetWardGroupName(bangsalId);
            await _hubContext.Clients.Group(groupName).SendAsync(
                WardCockpitContracts.OnBedStatusChangedEvent,
                payload,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "WardCockpit SignalR PublishBedStatusChanged failed for BangsalId {BangsalId}, BedId {BedId}",
                bangsalId, payload.BedId);
        }
    }

    public async Task PublishWaitingListUpdatedAsync(
        string bangsalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bangsalId))
            return;

        try
        {
            var groupName = WardCockpitContracts.GetWardGroupName(bangsalId);
            var payload = new WaitingListUpdatedPayload(bangsalId);
            await _hubContext.Clients.Group(groupName).SendAsync(
                WardCockpitContracts.OnWaitingListUpdatedEvent,
                payload,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "WardCockpit SignalR PublishWaitingListUpdated failed for BangsalId {BangsalId}",
                bangsalId);
        }
    }

    public async Task PublishKpiUpdatedAsync(
        string bangsalId,
        object kpi,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bangsalId))
            return;

        try
        {
            var groupName = WardCockpitContracts.GetWardGroupName(bangsalId);
            var payload = new KpiUpdatedPayload(kpi);
            await _hubContext.Clients.Group(groupName).SendAsync(
                WardCockpitContracts.OnKpiUpdatedEvent,
                payload,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "WardCockpit SignalR PublishKpiUpdated failed for BangsalId {BangsalId}",
                bangsalId);
        }
    }
}
