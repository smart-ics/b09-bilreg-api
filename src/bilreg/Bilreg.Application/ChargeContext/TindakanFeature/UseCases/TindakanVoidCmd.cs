using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record TindakanVoidCmd(string TindakanId, string UserId, string VoidReason,
    string ClientIpAddress, string UserAgent) : IRequest, ITindakanKey;

public class TindakanVoidHandler : IRequestHandler<TindakanVoidCmd>
{
    private readonly ITindakanRepo _tdkRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IJurnalRepo _jurnalRepo;
    public TindakanVoidHandler(ITindakanRepo tdkRepo,
        IAuditRepo auditRepo, ITglJamProvider tglJamProvider, 
        ITrsBillingRepo trsBillingRepo, 
        IJurnalRepo jurnalRepo)
    {
        _tdkRepo = tdkRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
        _trsBillingRepo = trsBillingRepo;
        _jurnalRepo = jurnalRepo;
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
        var occurredAt = _tglJamProvider.Now;
        tdk.Void(request.UserId, occurredAt);

        using var trans = TransHelper.NewScope();
        _tdkRepo.SaveChanges(tdk);
        _trsBillingRepo.DeleteEntity(TrsBillType.Key(tdk.TindakanId));
        _jurnalRepo.DeleteEntity(JurnalType.Key(tdk.TindakanId));

        var audit = CreateAudit(tdk, snapshotJson, request);
        _auditRepo.SaveChanges(audit);
        trans.Complete();

        return Task.CompletedTask;
    }
    
    private static AuditLog CreateAudit(TindakanModel tdk, string snapShotJson, TindakanVoidCmd cmd)
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
            userAgent: cmd.UserAgent);
        
        return result;
    }
}
