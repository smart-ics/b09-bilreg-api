using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.SalesContext.PenjualanFeature.UseCases;

public record PenjualanPoliVoidCmd(
    string TransaksiId,
    string UserId,
    string VoidReason,
    string ClientIpAddress,
    string UserAgent
) : IRequest, IPenjualanKey
{
    public string PenjualanId => TransaksiId.Split('-')[0].Trim();
}

public class PenjualanPoliVoidHandler : IRequestHandler<PenjualanPoliVoidCmd>
{
    private readonly IPenjualanRepo _penjualanRepo;
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IJurnalRepo _jurnalRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IMediator _mediator;

    public PenjualanPoliVoidHandler(
        IPenjualanRepo penjualanRepo,
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IJurnalRepo jurnalRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider,
        IMediator mediator)
    {
        _penjualanRepo = penjualanRepo;
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _jurnalRepo = jurnalRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
        _mediator = mediator;
    }

    public async Task Handle(PenjualanPoliVoidCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TransaksiId, nameof(request.TransaksiId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.VoidReason, nameof(request.VoidReason));

        var penjualanId = request.PenjualanId;
        var penjualan = _penjualanRepo.LoadEntity(PenjualanModel.Key(penjualanId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Penjualan '{penjualanId}' not found")
            );

        if (penjualan.AuditTrail.IsVoided)
            throw new InvalidOperationException($"Penjualan '{penjualanId}' sudah void.");

        if (!string.IsNullOrWhiteSpace(penjualan.ResepId) && penjualan.ResepId != "-")
            throw new InvalidOperationException($"Penjualan resep '{penjualan.ResepId}' tidak dapat dibatalkan dari modul poliklinik.");

        var regKey = RegModel.Key(penjualan.Register.RegId);
        var tataRekening = _tataRekeningRepo.LoadEntity(regKey).Match(
            onSome: x => x,
            onNone: () => (TataRekeningModel?)null
        );

        if (tataRekening is not null)
        {
            foreach (var item in penjualan.ListItem)
            {
                tataRekening.DeleteBill(item.PenjualanItemId);
            }
        }

        var snapshotJson = AuditLogSnapshotJson.Serialize(penjualan);
        var occurredAt = _tglJamProvider.Now;

        using var trans = TransHelper.NewScope();

        var voidTrsReffId = penjualan.PenjualanId.Length <= 8
            ? $"{penjualan.PenjualanId}_V"
            : $"{penjualan.PenjualanId[..8]}_V";

        var voidCmd = new PostSaleVoidConsequenceCommand(
            penjualan.PenjualanId,
            voidTrsReffId,
            occurredAt,
            request.UserId);

        var voidResult = await _mediator.Send(voidCmd, cancellationToken);
        if (voidResult.Outcome != PostSaleVoidOutcomeEnum.Success && voidResult.Outcome != PostSaleVoidOutcomeEnum.Idempotent)
        {
            throw new InvalidOperationException(
                $"Pengembalian stok gagal ({voidResult.Outcome}). Alasan: {voidResult.InconsistencyReason}");
        }

        penjualan.Void(request.UserId, occurredAt);
        _penjualanRepo.SaveChanges(penjualan);

        if (tataRekening is not null)
        {
            _tataRekeningRepo.SaveChanges(tataRekening);
        }

        foreach (var item in penjualan.ListItem)
        {
            _trsBillingRepo.DeleteEntity(TrsBillType.Key(item.PenjualanItemId));
            _jurnalRepo.DeleteEntity(JurnalType.Key(item.PenjualanItemId));
        }

        var audit = AuditLog.Create(
            penjualan.AuditTrail.Voided,
            actionType: "VOID",
            entityName: nameof(PenjualanModel),
            entityId: penjualan.PenjualanId,
            reason: request.VoidReason,
            originalDataJson: snapshotJson,
            correlationId: penjualan.Register.RegId,
            clientIpAddress: request.ClientIpAddress,
            userAgent: request.UserAgent);

        _auditRepo.SaveChanges(audit);

        trans.Complete();
    }
}
