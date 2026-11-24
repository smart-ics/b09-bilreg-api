namespace Bilreg.Application.ApotekContext.QueueFeature;

/// <summary>
/// Port abstraksi untuk menyiarkan event hint-based antrean farmasi ke SignalR hub.
/// </summary>
public interface IPharmacyQueuePublisher
{
    /// <summary>
    /// Menyiarkan hint pembaruan worklist petugas antrean pada depo farmasi tertentu.
    /// </summary>
    Task PublishWorklistRefresh(string depoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Menyiarkan hint pembaruan tampilan dan pemicu pemanggilan audio pada monitor display ruang tunggu.
    /// </summary>
    Task PublishDisplayRefresh(string loketKey, long announcementVersion, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementasi dummy / no-op publisher untuk pengujian unit.
/// </summary>
public sealed class NullPharmacyQueuePublisher : IPharmacyQueuePublisher
{
    public Task PublishWorklistRefresh(string depoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PublishDisplayRefresh(string loketKey, long announcementVersion, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
