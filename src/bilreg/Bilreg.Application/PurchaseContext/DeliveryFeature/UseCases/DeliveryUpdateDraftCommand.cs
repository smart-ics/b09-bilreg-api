using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryUpdateDraftCommand(
    string DeliveryOrderId,
    string SuratJalanNo,
    string PoId,
    string SupplierId,
    string GudangId,
    string Keterangan,
    string UserId,
    IEnumerable<DeliveryUpdateDraftItemCommand> Items,
    string SupplierName = "",
    string GudangName = "",
    decimal BiayaLain = 0m,
    decimal DiskonLain = 0m,
    DateTime? DeliveryOrderDate = null
) : IRequest<DeliveryUpdateDraftResponse>, IDeliveryOrderKey
{
    public string DoId => DeliveryOrderId;
}

public record DeliveryUpdateDraftItemCommand(
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

public record DeliveryUpdateDraftResponse(
    string DeliveryOrderId,
    string Status = "Draft"
)
{
    public string DoId => DeliveryOrderId;
}

public class DeliveryUpdateDraftHandler : IRequestHandler<DeliveryUpdateDraftCommand, DeliveryUpdateDraftResponse>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;
    private readonly IDeliveryValidationDal _deliveryValidationDal;
    private readonly IPoDeliveryTrackingDal _poDeliveryTrackingDal;
    private readonly ITglJamProvider _tglJamProvider;

    public DeliveryUpdateDraftHandler(
        IDeliveryOrderRepo deliveryOrderRepo,
        IDeliveryValidationDal deliveryValidationDal,
        IPoDeliveryTrackingDal poDeliveryTrackingDal,
        ITglJamProvider tglJamProvider)
    {
        _deliveryOrderRepo = deliveryOrderRepo;
        _deliveryValidationDal = deliveryValidationDal;
        _poDeliveryTrackingDal = poDeliveryTrackingDal;
        _tglJamProvider = tglJamProvider;
    }

    public DeliveryUpdateDraftHandler(
        IDeliveryOrderRepo deliveryOrderRepo,
        IDeliveryValidationDal deliveryValidationDal,
        IPoDeliveryTrackingDal poDeliveryTrackingDal)
        : this(deliveryOrderRepo, deliveryValidationDal, poDeliveryTrackingDal, new DefaultTglJamProvider())
    {
    }

    private sealed class DefaultTglJamProvider : ITglJamProvider
    {
        public DateTime Now => DateTime.Now;
    }

    public Task<DeliveryUpdateDraftResponse> Handle(DeliveryUpdateDraftCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.DeliveryOrderId, nameof(request.DeliveryOrderId));
        Guard.Against.NullOrWhiteSpace(request.SuratJalanNo, nameof(request.SuratJalanNo));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var items = request.Items?.ToList() ?? [];
        if (items.Count == 0)
            throw new ArgumentException("Daftar item draf penerimaan barang tidak boleh kosong.", nameof(request.Items));

        // 2. Load Existing Entity
        var key = DeliveryOrderModel.Key(request.DeliveryOrderId);
        var model = _deliveryOrderRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokumen penerimaan barang '{request.DeliveryOrderId}' tidak ditemukan.")
            );

        // 3. Status Validation (Only Draft allowed)
        if (model.Status == DeliveryOrderStatusEnum.Confirmed)
            throw new InvalidOperationException("Dokumen sudah berstatus Confirmed.");
        if (model.Status == DeliveryOrderStatusEnum.Voided)
            throw new InvalidOperationException("Dokumen yang sudah dibatalkan tidak dapat diubah.");
        if (model.Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Hanya dokumen berstatus Draft yang dapat diubah. Status saat ini: {model.Status}.");

        // 4. Validate Surat Jalan Uniqueness excluding current DO (TD-10)
        var supplierId = !string.IsNullOrWhiteSpace(request.SupplierId) ? request.SupplierId : model.Supplier.PartnerId;
        var isUnique = _deliveryValidationDal.IsSuratJalanUnique(supplierId, request.SuratJalanNo, request.DeliveryOrderId);
        if (!isUnique)
        {
            throw new InvalidOperationException(
                $"Surat Jalan Vendor '{request.SuratJalanNo}' dari pemasok '{supplierId}' sudah pernah diterima.");
        }

        // 5. Validate PO Quota & Destination Warehouse (TD-07)
        var poId = !string.IsNullOrWhiteSpace(request.PoId) ? request.PoId.Trim() : model.PoId?.Trim() ?? string.Empty;
        var hasPo = !string.IsNullOrWhiteSpace(poId);
        if (hasPo)
        {
            var poQuotas = _poDeliveryTrackingDal.GetRemainingQuota(poId)?.ToList() ?? [];
            if (poQuotas.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Surat Pesanan '{poId}' tidak ditemukan atau tidak memiliki item aktif.");
            }

            var gudangId = !string.IsNullOrWhiteSpace(request.GudangId) ? request.GudangId : model.Gudang.LayananId;
            var poLayananId = poQuotas.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.LayananId))?.LayananId;
            if (!string.IsNullOrWhiteSpace(poLayananId) && !string.Equals(poLayananId, gudangId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Gudang penerima '{gudangId}' tidak sesuai dengan gudang tujuan PO '{poLayananId}'.");
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

        // 6. Update Model Header Properties & Audit Trail
        var modifiedAt = _tglJamProvider.Now;
        model.UpdateSuratJalanNo(request.SuratJalanNo);
        model.UpdateKeterangan(request.Keterangan ?? string.Empty);
        model.SetBiayaLain(request.BiayaLain);
        model.SetDiskonLain(request.DiskonLain);

        if (request.DeliveryOrderDate.HasValue)
        {
            var updatedDate = request.DeliveryOrderDate.Value.TimeOfDay == TimeSpan.Zero
                ? request.DeliveryOrderDate.Value.Date.Add(model.DeliveryOrderDate.TimeOfDay != TimeSpan.Zero ? model.DeliveryOrderDate.TimeOfDay : modifiedAt.TimeOfDay)
                : request.DeliveryOrderDate.Value;
            model.UpdateDeliveryOrderDate(updatedDate);
        }

        if (!string.IsNullOrWhiteSpace(request.SupplierId) && request.SupplierId != model.Supplier.PartnerId)
            model.UpdateSupplier(new PartnerReff(request.SupplierId, request.SupplierName ?? string.Empty));

        if (!string.IsNullOrWhiteSpace(request.GudangId) && request.GudangId != model.Gudang.LayananId)
            model.UpdateGudang(new LayananReff(request.GudangId, request.GudangName ?? string.Empty));

        if (poId != model.PoId)
            model.UpdatePoId(poId);

        model.SetModified(request.UserId, modifiedAt);

        // 7. Clear Existing Items and Add Updated Items
        model.ClearItems();

        var refDate = model.DeliveryOrderDate;
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
                refDate);
        }

        // 8. Save Changes (Updates TB_TRS_DO, replaces TB_TRS_DO2, updates BILRG_DoInspeksi)
        using var trans = TransHelper.NewScope();
        _deliveryOrderRepo.SaveChanges(model);
        trans.Complete();

        return Task.FromResult(new DeliveryUpdateDraftResponse(model.DeliveryOrderId, model.Status.ToString()));
    }
}
