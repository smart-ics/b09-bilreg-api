namespace Bilreg.Application.BedUsageContext.WardCockpitFeature;

public record BedStatusChangedPayload(
    string BedId,
    string NewStatus,
    object? CurrentOccupancy = null);

public record WaitingListUpdatedPayload(
    string BangsalId);

public record KpiUpdatedPayload(
    object Kpi);

public interface IWardCockpitEventPublisher
{
    Task PublishBedStatusChangedAsync(
        string bangsalId,
        string bedId,
        string newStatus,
        object? currentOccupancy = null,
        CancellationToken cancellationToken = default);

    Task PublishBedStatusChangedAsync(
        string bangsalId,
        BedStatusChangedPayload payload,
        CancellationToken cancellationToken = default);

    Task PublishWaitingListUpdatedAsync(
        string bangsalId,
        CancellationToken cancellationToken = default);

    Task PublishKpiUpdatedAsync(
        string bangsalId,
        object kpi,
        CancellationToken cancellationToken = default);

    // Convenience method aliases matching event names
    Task OnBedStatusChanged(
        string bangsalId,
        string bedId,
        string newStatus,
        object? currentOccupancy = null,
        CancellationToken cancellationToken = default) =>
        PublishBedStatusChangedAsync(bangsalId, bedId, newStatus, currentOccupancy, cancellationToken);

    Task OnWaitingListUpdated(
        string bangsalId,
        CancellationToken cancellationToken = default) =>
        PublishWaitingListUpdatedAsync(bangsalId, cancellationToken);

    Task OnKpiUpdated(
        string bangsalId,
        object kpi,
        CancellationToken cancellationToken = default) =>
        PublishKpiUpdatedAsync(bangsalId, kpi, cancellationToken);
}

public sealed class NullWardCockpitEventPublisher : IWardCockpitEventPublisher
{
    public Task PublishBedStatusChangedAsync(
        string bangsalId,
        string bedId,
        string newStatus,
        object? currentOccupancy = null,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishBedStatusChangedAsync(
        string bangsalId,
        BedStatusChangedPayload payload,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishWaitingListUpdatedAsync(
        string bangsalId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PublishKpiUpdatedAsync(
        string bangsalId,
        object kpi,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
