using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryDirectConfirmCommand(
    string SuratJalanNo,
    string PoId,
    string SupplierId,
    string GudangId,
    string Keterangan,
    string UserId,
    IEnumerable<DeliveryDirectConfirmItemCommand> Items,
    string SupplierName = "",
    string GudangName = "",
    decimal BiayaLain = 0m,
    decimal DiskonLain = 0m,
    DateTime? DeliveryOrderDate = null,
    DateTime? ConfirmedAt = null
) : IRequest<DeliveryDirectConfirmResponse>;

public record DeliveryDirectConfirmItemCommand(
    string BrgId,
    string SatuanId,
    int Konversi,
    decimal Harga,
    decimal Diskon,
    decimal TaxPercentage,
    string NoBatch,
    DateTime TglEd,
    decimal QtyKirim,
    decimal QtyDiterima,
    decimal QtyDitolak,
    string AlasanTolak = "",
    string BrgName = "",
    string SatuanName = "",
    bool IsBonus = false,
    bool HasJaminanRetur = false,
    string NoSuratJaminan = "",
    int PoNoUrut = 0
);

public record DeliveryDirectConfirmResponse(
    string DeliveryOrderId,
    string Status
)
{
    public string DoId => DeliveryOrderId;
}

public class DeliveryDirectConfirmHandler : IRequestHandler<DeliveryDirectConfirmCommand, DeliveryDirectConfirmResponse>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;
    private readonly IDeliveryValidationDal _deliveryValidationDal;
    private readonly IPoDeliveryTrackingDal _poDeliveryTrackingDal;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;

    public DeliveryDirectConfirmHandler(
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

    public DeliveryDirectConfirmHandler(
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

    public async Task<DeliveryDirectConfirmResponse> Handle(DeliveryDirectConfirmCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SuratJalanNo, nameof(request.SuratJalanNo));
        Guard.Against.NullOrWhiteSpace(request.SupplierId, nameof(request.SupplierId));
        Guard.Against.NullOrWhiteSpace(request.GudangId, nameof(request.GudangId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var items = request.Items?.ToList() ?? [];
        if (items.Count == 0)
            throw new ArgumentException("Daftar item penerimaan barang tidak boleh kosong.", nameof(request.Items));

        // 2. Validate Surat Jalan Uniqueness (TD-06)
        var isUnique = _deliveryValidationDal.IsSuratJalanUnique(request.SupplierId, request.SuratJalanNo);
        if (!isUnique)
        {
            throw new InvalidOperationException(
                $"Surat Jalan Vendor '{request.SuratJalanNo}' dari pemasok '{request.SupplierId}' sudah pernah diterima.");
        }

        // 3. Validate PO Receiving Cap and Destination Warehouse (TD-07)
        var poId = request.PoId?.Trim() ?? string.Empty;
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

            var poLayananId = poQuotas.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.LayananId))?.LayananId;
            if (!string.IsNullOrWhiteSpace(poLayananId) && !string.Equals(poLayananId, request.GudangId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Gudang penerima '{request.GudangId}' tidak sesuai dengan gudang tujuan PO '{poLayananId}'.");
            }

            // Receiving Cap for non-bonus items
            var nonBonusItems = items.Where(x => !x.IsBonus).ToList();

            var groupedByPoLine = nonBonusItems
                .Where(x => x.PoNoUrut > 0)
                .GroupBy(x => x.PoNoUrut);

            foreach (var group in groupedByPoLine)
            {
                var noUrut = group.Key;
                var quota = poQuotas.FirstOrDefault(q => q.NoUrut == noUrut);
                if (quota == null)
                {
                    throw new InvalidOperationException(
                        $"Item PO baris ke-{noUrut} tidak ditemukan pada Surat Pesanan '{poId}'.");
                }

                var totalDiterima = group.Sum(x => x.QtyDiterima);
                if (totalDiterima > quota.SisaQty)
                {
                    throw new InvalidOperationException(
                        $"Kuantitas diterima ({totalDiterima}) untuk barang '{quota.BrgName}' ({quota.BrgId}) melebihi sisa kuota pesanan ({quota.SisaQty}).");
                }
            }

            var groupedByBrg = nonBonusItems
                .Where(x => x.PoNoUrut <= 0)
                .GroupBy(x => x.BrgId);

            foreach (var group in groupedByBrg)
            {
                var brgId = group.Key;
                var quota = poQuotas.FirstOrDefault(q => q.BrgId == brgId);
                if (quota == null)
                {
                    throw new InvalidOperationException(
                        $"Barang '{brgId}' tidak ditemukan pada Surat Pesanan '{poId}'.");
                }

                var totalDiterima = group.Sum(x => x.QtyDiterima);
                if (totalDiterima > quota.SisaQty)
                {
                    throw new InvalidOperationException(
                        $"Kuantitas diterima ({totalDiterima}) untuk barang '{quota.BrgName}' ({quota.BrgId}) melebihi sisa kuota pesanan ({quota.SisaQty}).");
                }
            }
        }

        // 4. Build Model
        var now = _tglJamProvider.Now;
        var occuredAt = request.DeliveryOrderDate.HasValue
            ? (request.DeliveryOrderDate.Value.TimeOfDay == TimeSpan.Zero
                ? request.DeliveryOrderDate.Value.Date.Add(now.TimeOfDay)
                : request.DeliveryOrderDate.Value)
            : now;
        var supplier = new PartnerReff(request.SupplierId, request.SupplierName ?? string.Empty);
        var gudang = new LayananReff(request.GudangId, request.GudangName ?? string.Empty);

        var model = DeliveryOrderModel.Create(
            request.SuratJalanNo,
            poId,
            supplier,
            gudang,
            request.Keterangan ?? string.Empty,
            request.UserId,
            occuredAt);

        if (request.BiayaLain > 0)
            model.SetBiayaLain(request.BiayaLain);
        if (request.DiskonLain > 0)
            model.SetDiskonLain(request.DiskonLain);

        foreach (var item in items)
        {
            var inspection = DeliveryInspectionType.Create(
                item.QtyKirim,
                item.QtyDiterima,
                item.QtyDitolak,
                item.AlasanTolak,
                item.IsBonus,
                item.HasJaminanRetur,
                item.NoSuratJaminan,
                poId,
                item.PoNoUrut);

            var brg = new BrgReff(item.BrgId, string.IsNullOrWhiteSpace(item.BrgName) ? item.BrgId : item.BrgName);
            var satuan = new SatuanType(item.SatuanId, string.IsNullOrWhiteSpace(item.SatuanName) ? item.SatuanId : item.SatuanName);

            model.AddItem(
                brg,
                satuan,
                item.Konversi,
                item.Harga,
                item.Diskon,
                item.TaxPercentage,
                item.NoBatch,
                item.TglEd,
                inspection,
                occuredAt);
        }

        // 5. Atomic Unit of Work (TD-05)
        var confirmedAt = request.ConfirmedAt ?? _tglJamProvider.Now;
        using var trans = TransHelper.NewScope();

        // 5a. Post Goods Receipt Consequence (UC-STL-001) for each item with QtyDiterima > 0
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
                    NoBatch: item.NoBatch);

                var stockResult = await _mediator.Send(stockCmd, cancellationToken);
                if (stockResult == null || stockResult.Outcome == PostGoodsReceiptOutcomeEnum.AbortedInconsistent)
                {
                    var reason = stockResult?.InconsistencyReason ?? "Gagal memposting saldo kartu stok.";
                    throw new InvalidOperationException($"Posting stok untuk barang '{item.Brg.BrgId}' gagal: {reason}");
                }
            }
        }

        // 5b. Update PO Fulfillment Quota & Status
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

        // 5c. Confirm and Save Changes
        model.Confirm(request.UserId, confirmedAt);
        _deliveryOrderRepo.SaveChanges(model);

        // 5d. Commit Atomic Scope
        trans.Complete();

        return new DeliveryDirectConfirmResponse(model.DeliveryOrderId, model.Status.ToString());
    }
}
