using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Application.Shared.BillingLockFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record TindakanVoidCmd(string TindakanId, string UserId, string VoidReason,
    string ClientIpAddress, string UserAgent, string RegId = "") : IRequest, ITindakanKey;

public class TindakanVoidHandler : IRequestHandler<TindakanVoidCmd>
{
    private readonly ITindakanRepo _tdkRepo;
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IJurnalRepo _jurnalRepo;
    private readonly IBillingLockValidationService _billingLockValidator;

    public TindakanVoidHandler(ITindakanRepo tdkRepo,
        ITataRekeningRepo tataRekeningRepo,
        IAuditRepo auditRepo, 
        ITglJamProvider tglJamProvider, 
        ITrsBillingRepo trsBillingRepo, 
        IJurnalRepo jurnalRepo,
        IBillingLockValidationService billingLockValidator)
    {
        _tdkRepo = tdkRepo;
        _tataRekeningRepo = tataRekeningRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
        _trsBillingRepo = trsBillingRepo;
        _jurnalRepo = jurnalRepo;
        _billingLockValidator = billingLockValidator;
    }

    public async Task Handle(TindakanVoidCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TindakanId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrWhiteSpace(request.VoidReason);

        var tdk = _tdkRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tindakan {request.TindakanId} not found") 
            );

        var regId = !string.IsNullOrWhiteSpace(request.RegId) ? request.RegId : tdk.Reg.RegId;
        await _billingLockValidator.EnsureCanModifyBillingAsync(regId, cancellationToken);

        var tataRekening = _tataRekeningRepo.LoadEntity(RegModel.Key(tdk.Reg.RegId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tata Rekening '{tdk.Reg.RegId}' not found")
            );

        var snapshotJson = AuditLogSnapshotJson.Serialize(tdk);
        var occurredAt = _tglJamProvider.Now;
        tdk.Void(request.UserId, occurredAt);

        // Cari billing ID yang cocok di tataRekening (bisa exact match atau prefix match karena kolom legasi 10-char)
        var matchedBill = tataRekening.ListTrsBill
            .FirstOrDefault(x => string.Equals(x.TrsBillingId, tdk.TindakanId, StringComparison.OrdinalIgnoreCase)
                              || x.TrsBillingId.StartsWith(tdk.TindakanId, StringComparison.OrdinalIgnoreCase)
                              || tdk.TindakanId.StartsWith(x.TrsBillingId, StringComparison.OrdinalIgnoreCase));

        var billIdToDelete = matchedBill?.TrsBillingId ?? tdk.TindakanId;

        using var trans = TransHelper.NewScope();
        if (matchedBill != null)
        {
            tataRekening.DeleteBill(billIdToDelete);
            _tataRekeningRepo.SaveChanges(tataRekening);
        }
        else
        {
            tataRekening.EnsureCanDeleteBill();
        }

        _tdkRepo.SaveChanges(tdk);
        _trsBillingRepo.DeleteEntity(TrsBillType.Key(billIdToDelete));
        if (!string.Equals(billIdToDelete, tdk.TindakanId, StringComparison.OrdinalIgnoreCase))
        {
            _trsBillingRepo.DeleteEntity(TrsBillType.Key(tdk.TindakanId));
        }

        _jurnalRepo.DeleteEntity(JurnalType.Key(billIdToDelete));
        if (!string.Equals(billIdToDelete, tdk.TindakanId, StringComparison.OrdinalIgnoreCase))
        {
            _jurnalRepo.DeleteEntity(JurnalType.Key(tdk.TindakanId));
        }

        var audit = CreateAudit(tdk, snapshotJson, request);
        _auditRepo.SaveChanges(audit);
        trans.Complete();
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
