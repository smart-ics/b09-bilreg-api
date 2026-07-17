using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;

public record AdmCloseWaitingListCmd(
    string WaitingListId,
    string UserId) : IRequest, IWaitingListKey;

public class AdmCloseWaitingListHandler : IRequestHandler<AdmCloseWaitingListCmd>
{
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly IAuditRepo _auditRepo;

    public AdmCloseWaitingListHandler(
        IWaitingListRepo waitingListRepo,
        IAuditRepo auditRepo)
    {
        _waitingListRepo = waitingListRepo;
        _auditRepo = auditRepo;
    }

    public Task Handle(AdmCloseWaitingListCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.WaitingListId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var waitingList = _waitingListRepo.LoadEntity(request)
            .GetValueOrThrow($"Waiting List '{request.WaitingListId}' tidak ditemukan.");
        var snapshotJson = AuditLogSnapshotJson.Serialize(waitingList);
        var closed = waitingList.Close(request.UserId);

        _waitingListRepo.SaveChanges(closed);

        _auditRepo.SaveChanges(AuditLog.Create(
            closed.AuditTrail.Modified,
            "UPDATE",
            nameof(WaitingListModel),
            closed.WaitingListId,
            originalDataJson: snapshotJson));

        return Task.CompletedTask;
    }
}
