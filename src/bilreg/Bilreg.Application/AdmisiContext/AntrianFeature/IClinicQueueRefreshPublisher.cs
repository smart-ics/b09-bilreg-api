namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record ClinicQueueRefreshHint(string PoliId, string DokterId);

public interface IClinicQueueRefreshPublisher
{
    Task PublishRefreshHint(string poliId, string dokterId, CancellationToken cancellationToken = default);
}

public sealed class NullClinicQueueRefreshPublisher : IClinicQueueRefreshPublisher
{
    public Task PublishRefreshHint(string poliId, string dokterId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
