using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.BedIgdFeature;
using Bilreg.Application.IgdContext.BhpIgdFeature;
using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitVoidCmd(string IgdVisitId, string UserId, string VoidReason,
    string ClientIpAddress, string UserAgent)
    : IRequest<IgdVisitVoidResponse>, IIgdVisitKey;

public record IgdVisitVoidResponse(
    string IgdVisitId,
    bool IsVoided,
    bool BedReleased);

public class IgdVisitVoidHandler : IRequestHandler<IgdVisitVoidCmd, IgdVisitVoidResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IBedIgdRepo _bedIgdRepo;
    private readonly IPakaiBedRepo _pakaiBedRepo;
    private readonly ITindakanIgdRepo _tindakanRepo;
    private readonly IBhpIgdRepo _bhpRepo;
    private readonly IAuditRepo _auditRepo;

    public IgdVisitVoidHandler(
        IIgdVisitRepo igdVisitRepo,
        IBedIgdRepo bedIgdRepo,
        IPakaiBedRepo pakaiBedRepo,
        ITindakanIgdRepo tindakanRepo,
        IBhpIgdRepo bhpRepo,
        IAuditRepo auditRepo)
    {
        _igdVisitRepo = igdVisitRepo;
        _bedIgdRepo = bedIgdRepo;
        _pakaiBedRepo = pakaiBedRepo;
        _tindakanRepo = tindakanRepo;
        _bhpRepo = bhpRepo;
        _auditRepo = auditRepo;
    }

    public Task<IgdVisitVoidResponse> Handle(IgdVisitVoidCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.VoidReason, nameof(request.VoidReason));

        var visit = _igdVisitRepo.LoadEntity(request)
            .GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");

        if (visit.IsVoided)
        {
            return Task.FromResult(new IgdVisitVoidResponse(visit.IgdVisitId, true, BedReleased: false));
        }
        var snapshotJson = AuditLogSnapshotJson.Serialize(visit);

        var hasTindakan = _tindakanRepo.AnyForVisit(visit);
        var hasBhp = _bhpRepo.AnyForVisit(visit);

        var audit = new AuditInfoType(request.UserId, DateTime.Now);

        BedIgdModel? bed = null;
        PakaiBedModel? pakaiBed = null;
        var bedReleased = false;

        if (visit.HasObserved)
        {
            bed = _bedIgdRepo.LoadEntity(BedIgdModel.Key(visit.BedId))
                .GetValueOrThrow($"BedIgd '{visit.BedId}' not found (referenced by visit).");
            if (bed.CurrentIgdVisitId != visit.IgdVisitId)
                throw new InvalidOperationException(
                    $"Bed '{bed.BedIgdId}' tidak ditempati oleh visit '{visit.IgdVisitId}'.");

            pakaiBed = _pakaiBedRepo.LoadOpenForBed(bed)
                .GetValueOrThrow($"PakaiBed terbuka untuk bed '{bed.BedIgdId}' tidak ditemukan.");

            bed.Release(audit);
            pakaiBed.Close(audit);
            visit.ClearBed(audit);
            bedReleased = true;
        }

        visit.Void(hasTindakan, hasBhp, audit);

        using (var trans = TransHelper.NewScope())
        {
            if (bed is not null) _bedIgdRepo.SaveChanges(bed);
            if (pakaiBed is not null) _pakaiBedRepo.SaveChanges(pakaiBed);
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
        }

        var auditLog = CreateAudit(visit, snapshotJson, request);
        _auditRepo.SaveChanges(auditLog);

        return Task.FromResult(new IgdVisitVoidResponse(visit.IgdVisitId, visit.IsVoided, bedReleased));
    }

    private AuditLog CreateAudit(IgdVisitModel visit, string snapShotJson, IgdVisitVoidCmd cmd)
    {
        var result = AuditLog.Create(
            visit.AuditTrail.Voided,
            actionType: "VOID",
            entityName: nameof(IgdVisitModel),
            entityId: visit.IgdVisitId,
            reason: cmd.VoidReason,
            originalDataJson: snapShotJson,
            correlationId: visit.Reg.RegId,
            clientIpAddress: cmd.ClientIpAddress,
            userAgent: cmd.UserAgent
            );
        return result;
    }
}
