using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryCreateDraftCommand(
    string SuratJalanNo,
    string PoId,
    string SupplierId,
    string GudangId,
    string Keterangan,
    string UserId,
    IEnumerable<DeliveryCreateDraftItemCommand> Items,
    string SupplierName = "",
    string GudangName = "",
    decimal BiayaLain = 0m,
    decimal DiskonLain = 0m,
    DateTime? DeliveryOrderDate = null
) : IRequest<DeliveryCreateDraftResponse>;

public record DeliveryCreateDraftItemCommand(
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

public record DeliveryCreateDraftResponse(string DeliveryOrderId)
{
    public string DoId => DeliveryOrderId;
}

public class DeliveryCreateDraftHandler : IRequestHandler<DeliveryCreateDraftCommand, DeliveryCreateDraftResponse>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;
    private readonly IDeliveryValidationDal _deliveryValidationDal;
    private readonly IPoDeliveryTrackingDal _poDeliveryTrackingDal;
    private readonly ITglJamProvider _tglJamProvider;

    public DeliveryCreateDraftHandler(
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

    public DeliveryCreateDraftHandler(
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

    public Task<DeliveryCreateDraftResponse> Handle(DeliveryCreateDraftCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SuratJalanNo, nameof(request.SuratJalanNo));
        Guard.Against.NullOrWhiteSpace(request.SupplierId, nameof(request.SupplierId));
        Guard.Against.NullOrWhiteSpace(request.GudangId, nameof(request.GudangId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var items = request.Items?.ToList() ?? [];
        if (items.Count == 0)
            throw new ArgumentException("Daftar item draf penerimaan barang tidak boleh kosong.", nameof(request.Items));

        // 2. Validate Surat Jalan Uniqueness (TD-06)
        var isUnique = _deliveryValidationDal.IsSuratJalanUnique(request.SupplierId, request.SuratJalanNo);
        if (!isUnique)
        {
            throw new InvalidOperationException(
                $"Surat Jalan Vendor '{request.SuratJalanNo}' dari pemasok '{request.SupplierId}' sudah pernah diterima.");
        }

        // 3. Validate PO Receiving Cap (TD-07, TD-17)
        var hasPo = !string.IsNullOrWhiteSpace(request.PoId);
        if (hasPo)
        {
            var poQuotas = _poDeliveryTrackingDal.GetRemainingQuota(request.PoId)?.ToList() ?? [];
            if (poQuotas.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Surat Pesanan '{request.PoId}' tidak ditemukan atau tidak memiliki item aktif.");
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
                        $"Item PO baris ke-{noUrut} tidak ditemukan pada Surat Pesanan '{request.PoId}'.");
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
                        $"Barang '{brgId}' tidak ditemukan pada Surat Pesanan '{request.PoId}'.");
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
        var poId = request.PoId?.Trim() ?? string.Empty;

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

        // 5. Save Changes (Status is Draft, no inventory movements or PO status updates triggered)
        _deliveryOrderRepo.SaveChanges(model);

        return Task.FromResult(new DeliveryCreateDraftResponse(model.DeliveryOrderId));
    }
}
