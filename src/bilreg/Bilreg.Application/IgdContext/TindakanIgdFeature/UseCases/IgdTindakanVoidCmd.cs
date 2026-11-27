using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Application.Shared.BillingLockFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
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
    private readonly IBillingLockValidationService _billingLockValidator;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IIgdStockService _igdStockService;

    public IgdTindakanVoidHandler(
        ITindakanIgdRepo tindakanIgdRepo,
        IAuditRepo auditRepo,
        IIgdVisitRepo igdVisitRepo,
        ITglJamProvider tglJamProvider,
        IBillingLockValidationService billingLockValidator,
        ITrsBillingRepo trsBillingRepo,
        IIgdStockService igdStockService)
    {
        _tindakanIgdRepo = tindakanIgdRepo;
        _auditRepo = auditRepo;
        _igdVisitRepo = igdVisitRepo;
        _tglJamProvider = tglJamProvider;
        _billingLockValidator = billingLockValidator;
        _trsBillingRepo = trsBillingRepo;
        _igdStockService = igdStockService;
    }

    public IgdTindakanVoidHandler(
        ITindakanIgdRepo tindakanIgdRepo,
        IAuditRepo auditRepo,
        IIgdVisitRepo igdVisitRepo,
        ITglJamProvider tglJamProvider,
        IBillingLockValidationService billingLockValidator,
        ITrsBillingRepo trsBillingRepo)
        : this(tindakanIgdRepo, auditRepo, igdVisitRepo, tglJamProvider, billingLockValidator, trsBillingRepo, new NullIgdStockService())
    {
    }

    public IgdTindakanVoidHandler(
        ITindakanIgdRepo tindakanIgdRepo,
        IAuditRepo auditRepo,
        IIgdVisitRepo igdVisitRepo,
        ITglJamProvider tglJamProvider)
        : this(tindakanIgdRepo, auditRepo, igdVisitRepo, tglJamProvider, new NullBillingLockValidationService(), new NullTrsBillingRepo(), new NullIgdStockService())
    {
    }

    private class NullIgdStockService : IIgdStockService
    {
        public Task<string> DeductBhpAsync(string layananId, string brgId, decimal qty, string userId, string keterangan, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);
        public Task<IgdBomProcessResult> ProcessTindakanBomAsync(string tindakanIgdId, string tarifId, int tindakanQty, string layananId, string regId, string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new IgdBomProcessResult(string.Empty, false, []));
        public Task VoidTindakanStockAsync(string tindakanIgdId, string headerPakaiBrgId, IEnumerable<string>? itemPakaiBrgIds, string userId, string voidReason, DateTime voidDate, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class NullTrsBillingRepo : ITrsBillingRepo
    {
        public void SaveChanges(TrsBillType entity) { }
        public void DeleteEntity(ITrsBillingKey key) { }
        public Nuna.Lib.PatternHelper.MayBe<TrsBillType> LoadEntity(ITrsBillingKey key) => Nuna.Lib.PatternHelper.MayBe<TrsBillType>.None;
        public IEnumerable<TrsBillView> ListData(Bilreg.Domain.AdmisiContext.RegFeature.IRegKey filter) => [];
        public IEnumerable<TrsBillType> ListEntity(Bilreg.Domain.AdmisiContext.RegFeature.IRegKey regKey) => [];
    }

    public async Task Handle(IgdTindakanVoidCmd request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.TindakanIgdId, nameof(request.TindakanIgdId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.VoidReason, nameof(request.VoidReason));

        // LOAD & BUILD
        var tdk = _tindakanIgdRepo.LoadEntity(request).GetValueOrThrow($"Tindakan IGD {request.TindakanIgdId} not found");

        if (tdk.IsPosted)
        {
            await _billingLockValidator.EnsureCanModifyBillingAsync(tdk.RegId, cancellationToken);
        }

        var snapshotJson = AuditLogSnapshotJson.Serialize(tdk);
        var visit = _igdVisitRepo.LoadEntity(IgdVisitModel.Key(tdk.IgdVisitId))
            .GetValueOrThrow($"IgdVisit '{tdk.IgdVisitId}' not found");
        var auditVoid = new AuditInfoType(request.UserId, _tglJamProvider.Now);

        tdk.MarkAsVoided(auditVoid, request.VoidReason);
        visit.RecordVoidTindakanEvent(tdk, auditVoid);
        
        // WRITE
        using (var trans = TransHelper.NewScope())
        {
            var itemPakaiBrgIds = tdk.ListBarang
                .Where(x => !string.IsNullOrWhiteSpace(x.PakaiBrgId))
                .Select(x => x.PakaiBrgId);

            await _igdStockService.VoidTindakanStockAsync(
                tdk.TindakanIgdId,
                tdk.PakaiBrgId,
                itemPakaiBrgIds,
                request.UserId,
                request.VoidReason,
                auditVoid.Timestamp,
                cancellationToken);

            if (tdk.IsPosted)
            {
                var billIdsToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(tdk.TrsBillId))
                    billIdsToDelete.Add(tdk.TrsBillId);
                if (!string.IsNullOrWhiteSpace(tdk.TrsTdkId))
                    billIdsToDelete.Add(tdk.TrsTdkId);

                foreach (var barang in tdk.ListBarang)
                {
                    if (!string.IsNullOrWhiteSpace(barang.TrsBillingId))
                        billIdsToDelete.Add(barang.TrsBillingId);
                    if (barang.IsChargeBarang)
                        billIdsToDelete.Add($"{tdk.TindakanIgdId}-B{barang.NoUrut:D2}");
                }

                foreach (var billId in billIdsToDelete)
                {
                    _trsBillingRepo.DeleteEntity(TrsBillType.Key(billId));
                }
            }

            // mark barang void & void tdk
            _tindakanIgdRepo.MarkBarangVoid(tdk);
            _tindakanIgdRepo.DeleteEntity(tdk);
            // auditLog
            var audit = CreateAudit(tdk, snapshotJson, request);
            _auditRepo.SaveChanges(audit);
            // add evet visitIgd
            _igdVisitRepo.SaveChanges(visit);
            
            trans.Complete();
        }
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
