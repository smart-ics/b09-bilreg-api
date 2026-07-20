using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;

public record AdmUpdateWaitingListCmd(
    string WaitingListId,
    int Priority,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest, IWaitingListKey;

public class AdmUpdateWaitingListHandler : IRequestHandler<AdmUpdateWaitingListCmd>
{
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly IWardAccommodationGateway _wardGateway;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmUpdateWaitingListHandler(
        IWaitingListRepo waitingListRepo,
        IWardAccommodationGateway wardGateway,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _waitingListRepo = waitingListRepo;
        _wardGateway = wardGateway;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(AdmUpdateWaitingListCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.WaitingListId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var kelas = _wardGateway.ResolveKelas(request.KelasId);
        var bangsal = _wardGateway.ResolveBangsal(request.BangsalId);

        var waitingList = _waitingListRepo.LoadEntity(request)
            .GetValueOrThrow($"Waiting List '{request.WaitingListId}' tidak ditemukan.");
        var snapshotJson = AuditLogSnapshotJson.Serialize(waitingList);
        var occurredAt = _tglJamProvider.Now;
        var updated = waitingList.Update(request.Priority, kelas, bangsal, request.UserId, occurredAt);

        _waitingListRepo.SaveChanges(updated);

        _auditRepo.SaveChanges(AuditLog.Create(
            updated.AuditTrail.Modified,
            "UPDATE",
            nameof(WaitingListModel),
            updated.WaitingListId,
            originalDataJson: snapshotJson));

        return Task.CompletedTask;
    }
}
