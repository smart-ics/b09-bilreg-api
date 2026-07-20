using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.TindakanIgdFeature.UseCases;

public record IgdTindakanVoidCmd(string TindakanIgdId, string UserId, string VoidReason,
    string ClientIpAddress, string UserAgent) : IRequest, ITindakanIgdKey;

public class IgdTindakanVoidHandler : IRequestHandler<IgdTindakanVoidCmd>
{
    private readonly ITindakanIgdRepo _tindakanIgdRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly ITglJamProvider _tglJamProvider;
    public IgdTindakanVoidHandler(ITindakanIgdRepo tindakanIgdRepo,
        IAuditRepo auditRepo,
        IIgdVisitRepo igdVisitRepo,
        ITglJamProvider tglJamProvider)
    {
        _tindakanIgdRepo = tindakanIgdRepo;
        _auditRepo = auditRepo;
        _igdVisitRepo = igdVisitRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(IgdTindakanVoidCmd request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.TindakanIgdId, nameof(request.TindakanIgdId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.VoidReason, nameof(request.VoidReason));

        // LOAD & BUILD
        var tdk = _tindakanIgdRepo.LoadEntity(request).GetValueOrThrow($"Tindakan IGD {request.TindakanIgdId} not found");
        var snapshotJson = AuditLogSnapshotJson.Serialize(tdk);
        var visit = _igdVisitRepo.LoadEntity(IgdVisitModel.Key(tdk.IgdVisitId))
            .GetValueOrThrow($"IgdVisit '{tdk.IgdVisitId}' not found");
        var auditVoid = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        visit.RecordVoidTindakanEvent(tdk, auditVoid);
        
        // WRITE
        using (var trans = TransHelper.NewScope())
        {
            // void tdk
            _tindakanIgdRepo.DeleteEntity(tdk);
            // auditLog
            var audit = CreateAudit(tdk, snapshotJson, request);
            _auditRepo.SaveChanges(audit);
            // add evet visitIgd
            _igdVisitRepo.SaveChanges(visit);
            
            trans.Complete();
        }
        // RETURN
        return Task.CompletedTask;
    }

    private AuditLog CreateAudit(TindakanIgdModel tdk, string snapShootJson, IgdTindakanVoidCmd cmd)
    {
        var result = AuditLog.Create(
            tdk.Audit,
            actionType: "DELETE",
            entityName: nameof(TindakanIgdModel),
            entityId: tdk.TindakanIgdId,
            reason: cmd.VoidReason,
            originalDataJson: snapShootJson,
            correlationId: tdk.IgdVisitId,
            clientIpAddress: cmd.ClientIpAddress,
            userAgent: cmd.UserAgent
            );
        return result;
    }

}
