using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAdmissionQueueOperationRepo
{
    bool TryCall(string antrianId, int noUrut, string loketKey, string userId, DateTime at);
    bool TryRecall(string antrianId, int noUrut, string loketKey, byte[] expectedRowVersion, string userId, DateTime at);
    bool TryStartService(string antrianId, int noUrut, string loketKey, byte[] expectedRowVersion, string userId, DateTime at);
    bool TryReturnToWaiting(string antrianId, int noUrut, string loketKey, byte[] expectedRowVersion, string userId, DateTime at);
    bool TryWithdraw(string antrianId, int noUrut, string reason, string userId, DateTime at,
        string? loketKey, byte[]? expectedRowVersion);
    bool TryRedirect(string originAntrianId, int originNoUrut, string userId, DateTime at,
        string? loketKey, byte[]? expectedRowVersion, AntrianModel targetQueue,
        AntrianEntryModel replacement);
}

public interface IAdmissionQueueRefreshPublisher
{
    Task PublishAsync(string? loketKey, CancellationToken cancellationToken);
}

public sealed class NullAdmissionQueueRefreshPublisher : IAdmissionQueueRefreshPublisher
{
    public Task PublishAsync(string? loketKey, CancellationToken cancellationToken) => Task.CompletedTask;
}
