using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryConfirmCommand(
    string DeliveryOrderId,
    string UserId,
    DateTime? ConfirmedAt = null
) : IRequest<DeliveryConfirmResponse>, IDeliveryOrderKey
{
    public string DoId => DeliveryOrderId;
}

public record DeliveryConfirmResponse(
    string DeliveryOrderId,
    string Status
)
{
    public string DoId => DeliveryOrderId;
}

public class DeliveryConfirmHandler : IRequestHandler<DeliveryConfirmCommand, DeliveryConfirmResponse>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;
    private readonly IDeliveryValidationDal _deliveryValidationDal;
    private readonly IPoDeliveryTrackingDal _poDeliveryTrackingDal;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;

    public DeliveryConfirmHandler(
        IDeliveryOrderRepo deliveryOrderRepo,
        IDeliveryValidationDal deliveryValidationDal,
        IPoDeliveryTrackingDal poDeliveryTrackingDal,
        IMediator mediator,
        ITglJamProvider tglJamProvider)
    {
        _deliveryOrderRepo = deliveryOrderRepo;
        _deliveryValidationDal = deliveryValidationDal;
        _poDeliveryTrackingDal = poDeliveryTrackingDal;
        _mediator = mediator;
        _tglJamProvider = tglJamProvider;
    }

    public DeliveryConfirmHandler(
        IDeliveryOrderRepo deliveryOrderRepo,
        IDeliveryValidationDal deliveryValidationDal,
        IPoDeliveryTrackingDal poDeliveryTrackingDal,
        IMediator mediator)
        : this(deliveryOrderRepo, deliveryValidationDal, poDeliveryTrackingDal, mediator, new DefaultTglJamProvider())
    {
    }

    private sealed class DefaultTglJamProvider : ITglJamProvider
    {
        public DateTime Now => DateTime.Now;
    }

    public async Task<DeliveryConfirmResponse> Handle(DeliveryConfirmCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.DeliveryOrderId, nameof(request.DeliveryOrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        // 2. Load Entity
        var key = DeliveryOrderModel.Key(request.DeliveryOrderId);
        var model = _deliveryOrderRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokumen penerimaan barang '{request.DeliveryOrderId}' tidak ditemukan.")
            );

        // 3. Status and Integrity Checks
        if (model.Status == DeliveryOrderStatusEnum.Confirmed)
            throw new InvalidOperationException("Dokumen sudah berstatus Confirmed.");
        if (model.Status == DeliveryOrderStatusEnum.Voided)
            throw new InvalidOperationException("Dokumen yang sudah dibatalkan tidak dapat dikonfirmasi.");
        if (model.Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Hanya dokumen berstatus Draft yang dapat dikonfirmasi. Status saat ini: {model.Status}.");

        if (!model.ListItem.Any())
            throw new InvalidOperationException("Tidak dapat mengonfirmasi dokumen yang tidak memiliki item.");

        // 4. Validate Surat Jalan Uniqueness (TD-06)
        var isUnique = _deliveryValidationDal.IsSuratJalanUnique(model.Supplier.PartnerId, model.SuratJalanNo, model.DeliveryOrderId);
        if (!isUnique)
        {
            throw new InvalidOperationException(
                $"Surat Jalan Vendor '{model.SuratJalanNo}' dari pemasok '{model.Supplier.PartnerId}' sudah pernah diterima.");
        }

        // 5. Validate PO Receiving Cap (TD-07, TD-17)
        var poId = model.PoId?.Trim() ?? string.Empty;
        var hasPo = !string.IsNullOrWhiteSpace(poId);
        List<PoQuotaDto> poQuotas = [];

        if (hasPo)
        {
            poQuotas = _poDeliveryTrackingDal.GetRemainingQuota(poId)?.ToList() ?? [];
            if (poQuotas.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Surat Pesanan '{poId}' tidak ditemukan atau tidak memiliki item aktif.");
            }


            // Receiving Cap for non-bonus items
            var nonBonusItems = model.ListItem.Where(x => !x.Inspection.IsBonus).ToList();

            var groupedByPoLine = nonBonusItems
                .Where(x => x.Inspection.PoNoUrut > 0)
                .GroupBy(x => x.Inspection.PoNoUrut);

            foreach (var group in groupedByPoLine)
            {
                var noUrut = group.Key;
                var quota = poQuotas.FirstOrDefault(q => q.NoUrut == noUrut);
                if (quota == null)
                {
                    throw new InvalidOperationException(
                        $"Item PO baris ke-{noUrut} tidak ditemukan pada Surat Pesanan '{poId}'.");
                }

                var totalDiterima = group.Sum(x => x.Inspection.QtyDiterima);
                if (totalDiterima > quota.SisaQty)
                {
                    throw new InvalidOperationException(
                        $"Kuantitas diterima ({totalDiterima}) untuk barang '{quota.BrgName}' ({quota.BrgId}) melebihi sisa kuota pesanan ({quota.SisaQty}).");
                }
            }

            var groupedByBrg = nonBonusItems
                .Where(x => x.Inspection.PoNoUrut <= 0)
                .GroupBy(x => x.Brg.BrgId);

            foreach (var group in groupedByBrg)
            {
                var brgId = group.Key;
                var quota = poQuotas.FirstOrDefault(q => q.BrgId == brgId);
                if (quota == null)
                {
                    throw new InvalidOperationException(
                        $"Barang '{brgId}' tidak ditemukan pada Surat Pesanan '{poId}'.");
                }

                var totalDiterima = group.Sum(x => x.Inspection.QtyDiterima);
                if (totalDiterima > quota.SisaQty)
                {
                    throw new InvalidOperationException(
                        $"Kuantitas diterima ({totalDiterima}) untuk barang '{quota.BrgName}' ({quota.BrgId}) melebihi sisa kuota pesanan ({quota.SisaQty}).");
                }
            }
        }

        // 6. Atomic Unit of Work (TD-05)
        var confirmedAt = request.ConfirmedAt ?? _tglJamProvider.Now;
        using var trans = TransHelper.NewScope();

        // 6a. Post Goods Receipt Consequence (UC-STL-001) for each item with QtyDiterima > 0
        foreach (var item in model.ListItem)
        {
            if (item.Inspection.QtyDiterima > 0)
            {
                var konversi = item.Konversi > 0 ? item.Konversi : 1;
                var hpp = item.Inspection.IsBonus
                    ? 0m
                    : (item.Harga - item.Diskon) / konversi;
                var qtyPersediaan = item.Inspection.QtyDiterima * konversi;
                var itemPoId = !string.IsNullOrWhiteSpace(item.Inspection.PoId) ? item.Inspection.PoId : poId;

                var matchedQuota = (item.Inspection.PoNoUrut > 0
                    ? poQuotas.FirstOrDefault(q => q.NoUrut == item.Inspection.PoNoUrut)
                    : null) ?? poQuotas.FirstOrDefault(q => q.BrgId == item.Brg.BrgId);

                var satuanPersediaan = konversi > 1
                    ? (!string.IsNullOrWhiteSpace(matchedQuota?.SatuanKecilId) ? matchedQuota.SatuanKecilId : item.Satuan.SatuanId)
                    : item.Satuan.SatuanId;

                var stockCmd = new PostGoodsReceiptConsequenceCommand(
                    BrgId: item.Brg.BrgId,
                    BrgMasukReffId: model.DeliveryOrderId,
                    LayananId: model.Gudang.LayananId,
                    Qty: qtyPersediaan,
                    Hpp: hpp,
                    TrsReffId: model.DeliveryOrderId,
                    TglMutasi: confirmedAt,
                    UserId: request.UserId,
                    TglEd: item.TglEd,
                    PoReffId: string.IsNullOrWhiteSpace(itemPoId) ? null : itemPoId,
                    NoBatch: item.NoBatch,
                    SatuanId: satuanPersediaan);

                var stockResult = await _mediator.Send(stockCmd, cancellationToken);
                if (stockResult == null || stockResult.Outcome == PostGoodsReceiptOutcomeEnum.AbortedInconsistent)
                {
                    var reason = stockResult?.InconsistencyReason ?? "Gagal memposting saldo kartu stok.";
                    throw new InvalidOperationException($"Posting stok untuk barang '{item.Brg.BrgId}' gagal: {reason}");
                }
            }
        }

        // 6b. Update PO Fulfillment Quota & Status
        if (hasPo)
        {
            var nonBonusItems = model.ListItem
                .Where(x => !x.Inspection.IsBonus && x.Inspection.QtyDiterima > 0)
                .ToList();

            var lineGroups = nonBonusItems
                .GroupBy(x =>
                {
                    var targetPoId = !string.IsNullOrWhiteSpace(x.Inspection.PoId) ? x.Inspection.PoId : poId;
                    var noUrut = x.Inspection.PoNoUrut;
                    if (noUrut <= 0)
                    {
                        var matched = poQuotas.FirstOrDefault(q => q.BrgId == x.Brg.BrgId);
                        if (matched != null)
                            noUrut = matched.NoUrut;
                    }
                    return (PoId: targetPoId, NoUrut: noUrut);
                });

            foreach (var group in lineGroups)
            {
                if (!string.IsNullOrWhiteSpace(group.Key.PoId) && group.Key.NoUrut > 0)
                {
                    var totalAccepted = group.Sum(x => x.Inspection.QtyDiterima);
                    _poDeliveryTrackingDal.IncrementReceivedQty(group.Key.PoId, group.Key.NoUrut, totalAccepted);
                }
            }

            _poDeliveryTrackingDal.UpdatePoOverallStatus(poId);
        }

        // 6c. Confirm and Save Changes
        model.Confirm(request.UserId, confirmedAt);
        _deliveryOrderRepo.SaveChanges(model);

        // 6d. Commit Atomic Scope
        trans.Complete();

        return new DeliveryConfirmResponse(model.DeliveryOrderId, model.Status.ToString());
    }
}
