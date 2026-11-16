using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
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
    private readonly ITindakanBarangRepo? _tindakanBarangRepo;
    private readonly ITindakanStokPendingRepo? _tindakanStokPendingRepo;
    private readonly IPakaiBrgInternalService? _pakaiBrgInternalService;

    public TindakanVoidHandler(ITindakanRepo tdkRepo,
        ITataRekeningRepo tataRekeningRepo,
        IAuditRepo auditRepo, 
        ITglJamProvider tglJamProvider, 
        ITrsBillingRepo trsBillingRepo, 
        IJurnalRepo jurnalRepo,
        IBillingLockValidationService billingLockValidator,
        ITindakanBarangRepo? tindakanBarangRepo = null,
        ITindakanStokPendingRepo? tindakanStokPendingRepo = null,
        IPakaiBrgInternalService? pakaiBrgInternalService = null)
    {
        _tdkRepo = tdkRepo;
        _tataRekeningRepo = tataRekeningRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
        _trsBillingRepo = trsBillingRepo;
        _jurnalRepo = jurnalRepo;
        _billingLockValidator = billingLockValidator;
        _tindakanBarangRepo = tindakanBarangRepo;
        _tindakanStokPendingRepo = tindakanStokPendingRepo;
        _pakaiBrgInternalService = pakaiBrgInternalService;
    }

    public TindakanVoidHandler(ITindakanRepo tdkRepo,
        ITataRekeningRepo tataRekeningRepo,
        IAuditRepo auditRepo, 
        ITglJamProvider tglJamProvider, 
        ITrsBillingRepo trsBillingRepo, 
        IJurnalRepo jurnalRepo)
        : this(tdkRepo, tataRekeningRepo, auditRepo, tglJamProvider, trsBillingRepo, jurnalRepo, new NullBillingLockValidationService(), null, null, null)
    {
    }

    public TindakanVoidHandler(ITindakanRepo tdkRepo,
        IAuditRepo auditRepo, ITglJamProvider tglJamProvider, 
        ITrsBillingRepo trsBillingRepo, 
        IJurnalRepo jurnalRepo,
        ITataRekeningRepo tataRekeningRepo)
        : this(tdkRepo, tataRekeningRepo, auditRepo, tglJamProvider, trsBillingRepo, jurnalRepo, new NullBillingLockValidationService(), null, null, null)
    {
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

        var regKey = RegModel.Key(tdk.Reg.RegId);
        var tataRekening = _tataRekeningRepo.LoadEntity(regKey).Match(
            onSome: x => x,
            onNone: () => (TataRekeningModel?)null
        );

        var snapshotJson = AuditLogSnapshotJson.Serialize(tdk);
        var occurredAt = _tglJamProvider.Now;
        tdk.Void(request.UserId, occurredAt);

        // Cari billing ID yang cocok di tataRekening (bisa exact match atau prefix match karena kolom legasi 10-char)
        var matchedBill = tataRekening?.ListTrsBill
            .FirstOrDefault(x => string.Equals(x.TrsBillingId, tdk.TindakanId, StringComparison.OrdinalIgnoreCase)
                              || x.TrsBillingId.StartsWith(tdk.TindakanId, StringComparison.OrdinalIgnoreCase)
                              || tdk.TindakanId.StartsWith(x.TrsBillingId, StringComparison.OrdinalIgnoreCase));

        var billIdToDelete = matchedBill?.TrsBillingId ?? tdk.TindakanId;

        using var trans = TransHelper.NewScope();
        if (tataRekening != null)
        {
            if (matchedBill != null)
            {
                tataRekening.DeleteBill(billIdToDelete);
            }
            else
            {
                tataRekening.EnsureCanDeleteBill();
            }
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

        // Cascading void for BOM items, stock, and bills
        if (_tindakanBarangRepo != null)
        {
            var bomItems = _tindakanBarangRepo.ListByTindakanId(tdk.TindakanId)
                .Where(x => !x.IsVoid)
                .ToList();
            if (bomItems.Count > 0)
            {
                // 1. Mark pending void
                _tindakanStokPendingRepo?.MarkVoidByTindakanId(tdk.TindakanId, occurredAt, request.UserId);

                // 2. Void unique PakaiBrgId
                if (_pakaiBrgInternalService != null)
                {
                    var uniquePakaiIds = bomItems
                        .Select(x => x.PakaiBrgId)
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Distinct()
                        .ToList();

                    foreach (var pakaiId in uniquePakaiIds)
                    {
                        await _pakaiBrgInternalService.VoidFromTindakan(
                            new PakaiBrgVoidFromTindakanCommand(pakaiId, request.UserId, request.VoidReason),
                            cancellationToken);
                    }
                }

                // 3. Delete barang TrsBilling & Jurnal
                foreach (var item in bomItems.Where(x => !string.IsNullOrWhiteSpace(x.TrsBillingId)))
                {
                    if (tataRekening != null)
                    {
                        try { tataRekening.DeleteBill(item.TrsBillingId); } catch { /* ignore if not present in list */ }
                    }
                    _trsBillingRepo.DeleteEntity(TrsBillType.Key(item.TrsBillingId));
                    _jurnalRepo.DeleteEntity(JurnalType.Key(item.TrsBillingId));
                }

                // 4. Mark BILRG_TindakanBarang void
                _tindakanBarangRepo.MarkVoid(tdk.TindakanId, request.UserId);
            }
        }

        if (tataRekening != null)
        {
            _tataRekeningRepo.SaveChanges(tataRekening);
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
