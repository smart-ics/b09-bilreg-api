using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record TindakanVoidCmd(string TindakanId, string UserId, string VoidReason,
    string ClientIpAddress, string UserAgent) : IRequest, ITindakanKey;

public class TindakanVoidHandler : IRequestHandler<TindakanVoidCmd>
{
    private readonly ITindakanRepo _tdkRepo;
    private readonly IAuditRepo _auditRepo;
    public TindakanVoidHandler(ITindakanRepo tdkRepo, 
        IAuditRepo auditRepo)
    {
        _tdkRepo = tdkRepo;
        _auditRepo = auditRepo;
    }

    public Task Handle(TindakanVoidCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TindakanId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrWhiteSpace(request.VoidReason);

        var tdk = _tdkRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tindakan {request.TindakanId} not found") 
            );

        var snapshotJson = AuditLogSnapshotJson.Serialize(tdk);

        tdk.Void(request.UserId);
        _tdkRepo.SaveChanges(tdk);

        var audit = CreateAudit(tdk, snapshotJson, request);
        _auditRepo.SaveChanges(audit);

        return Task.CompletedTask;
    }
    private AuditLog CreateAudit(TindakanModel tdk, string snapShotJson, TindakanVoidCmd cmd)
    {
        var result = AuditLog.Create(
            tdk.AuditTrail.Voided,
            actionType: "VOID",
            entityName: nameof(TindakanModel),
            entityId: tdk.TindakanId,
            reason: cmd.VoidReason,
            originalDataJson: snapShotJson,
            correlationId: tdk.Reg.RegId,
            clientIpAddress: cmd.ClientIpAddress,
            userAgent: cmd.UserAgent
            );
        return result;
    }
}
