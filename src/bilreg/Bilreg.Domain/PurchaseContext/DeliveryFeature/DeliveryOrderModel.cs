using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.PurchaseContext.DeliveryFeature;

public class DeliveryOrderModel : IDeliveryOrderKey
{
    private readonly List<DeliveryOrderItemType> _listItem;

    public DeliveryOrderModel(
        string id,
        string suratJalanNo,
        string poId,
        DateTime deliveryOrderDate,
        PartnerReff supplier,
        LayananReff gudang,
        string keterangan,
        DeliveryOrderStatusEnum status,
        decimal subTotal,
        decimal taxTotal,
        decimal diskonTotal,
        decimal total,
        decimal biayaLain,
        decimal diskonLain,
        decimal grandTotal,
        AuditInfoType confirmed,
        AuditTrailType auditTrail,
        IEnumerable<DeliveryOrderItemType> items,
        string poDate = "")
    {
        DeliveryOrderId = id;
        SuratJalanNo = suratJalanNo;
        PoId = poId;
        DeliveryOrderDate = deliveryOrderDate;
        Supplier = supplier;
        Gudang = gudang;
        Keterangan = keterangan;
        Status = status;
        SubTotal = subTotal;
        TaxTotal = taxTotal;
        DiskonTotal = diskonTotal;
        Total = total;
        BiayaLain = biayaLain;
        DiskonLain = diskonLain;
        GrandTotal = grandTotal;
        Confirmed = confirmed;
        AuditTrail = auditTrail;
        _listItem = [.. items];
        PoDate = poDate ?? string.Empty;
    }

    public static IDeliveryOrderKey Key(string id) => new DeliveryOrderModel(
        id, AppConst.DASH, AppConst.DASH, DateTime.MinValue,
        new PartnerReff(AppConst.DASH, AppConst.DASH),
        new LayananReff(AppConst.DASH, AppConst.DASH),
        AppConst.DASH, DeliveryOrderStatusEnum.Draft,
        0m, 0m, 0m, 0m, 0m, 0m, 0m,
        AuditInfoType.Default, AuditTrailType.Default, []);

    public static DeliveryOrderModel Create(
        string suratJalanNo,
        string poId,
        PartnerReff supplier,
        LayananReff gudang,
        string keterangan,
        string userId,
        DateTime occuredAt,
        string poDate = "")
    {
        if (string.IsNullOrWhiteSpace(suratJalanNo))
            throw new ArgumentException("Nomor surat jalan vendor tidak boleh kosong.", nameof(suratJalanNo));
        if (supplier == null || string.IsNullOrWhiteSpace(supplier.PartnerId))
            throw new ArgumentException("Supplier wajib ditentukan.", nameof(supplier));
        if (gudang == null || string.IsNullOrWhiteSpace(gudang.LayananId))
            throw new ArgumentException("Gudang tujuan wajib ditentukan.", nameof(gudang));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID tidak boleh kosong.", nameof(userId));

        var newId = NunaId.NewLegacy("DM", 'A');
        var auditTrail = AuditTrailType.Create(userId, occuredAt);

        return new DeliveryOrderModel(
            newId,
            suratJalanNo.Trim(),
            poId?.Trim() ?? string.Empty,
            occuredAt,
            supplier,
            gudang,
            keterangan ?? string.Empty,
            DeliveryOrderStatusEnum.Draft,
            0m, 0m, 0m, 0m, 0m, 0m, 0m,
            AuditInfoType.Default,
            auditTrail,
            [],
            poDate?.Trim() ?? string.Empty);
    }

    public static DeliveryOrderModel Create(
        string suratJalanNo,
        string poId,
        PartnerType supplier,
        LayananReff gudang,
        string keterangan,
        string userId,
        DateTime occuredAt,
        string poDate = "")
        => Create(suratJalanNo, poId, supplier?.ToReff() ?? throw new ArgumentNullException(nameof(supplier)), gudang, keterangan, userId, occuredAt, poDate);

    public string DeliveryOrderId { get; private set; }
    public string SuratJalanNo { get; private set; }
    public string PoId { get; private set; }
    public string PoDate { get; private set; }
    public DateTime DeliveryOrderDate { get; private set; }
    public DateTime TglDo => DeliveryOrderDate;
    public PartnerReff Supplier { get; private set; }
    public LayananReff Gudang { get; private set; }
    public string Keterangan { get; private set; }
    public DeliveryOrderStatusEnum Status { get; private set; }

    public decimal SubTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal DiskonTotal { get; private set; }
    public decimal Total { get; private set; }
    public decimal BiayaLain { get; private set; }
    public decimal DiskonLain { get; private set; }
    public decimal GrandTotal { get; private set; }

    public AuditInfoType Confirmed { get; private set; }
    public AuditTrailType AuditTrail { get; private set; }

    public IEnumerable<DeliveryOrderItemType> ListItem => _listItem;

    private void Recalculate()
    {
        SubTotal = _listItem.Sum(item => item.SubTotal);
        TaxTotal = _listItem.Sum(item => item.Tax);
        DiskonTotal = _listItem.Sum(item => item.Diskon * item.Inspection.QtyDiterima);
        Total = _listItem.Sum(item => item.Total);
        GrandTotal = Total + BiayaLain - DiskonLain;
    }

    public void AddItem(DeliveryOrderItemType item, DateTime? referenceDate = null)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat menambah item pada dokumen berstatus {Status}.");

        if (item == null)
            throw new ArgumentNullException(nameof(item));

        var isDuplicate = _listItem.Any(x =>
            x.Brg.BrgId == item.Brg.BrgId &&
            string.Equals(x.NoBatch, item.NoBatch, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
            throw new ArgumentException($"Barang '{item.Brg.BrgName}' dengan nomor batch '{item.NoBatch}' sudah ada dalam daftar item.");

        var refDate = referenceDate ?? DeliveryOrderDate;
        var today = refDate.Date;
        if (item.TglEd.Date <= today)
            throw new ArgumentException(
                $"Tanggal kedaluwarsa ({item.TglEd:yyyy-MM-dd}) tidak boleh kurang dari atau sama dengan hari ini ({today:yyyy-MM-dd}).");

        var threshold18Months = today.AddMonths(18);
        if (item.TglEd.Date < threshold18Months)
        {
            if (!item.Inspection.HasJaminanRetur || string.IsNullOrWhiteSpace(item.Inspection.NoSuratJaminan))
                throw new ArgumentException(
                    $"Barang dengan sisa masa simpan kurang dari 18 bulan (ED: {item.TglEd:yyyy-MM-dd}) wajib menyertakan surat jaminan retur vendor dan nomor surat jaminan.");
        }

        var noUrut = _listItem.Select(x => x.NoUrut).DefaultIfEmpty(0).Max() + 1;
        item.SetNoUrut(noUrut);

        _listItem.Add(item);
        Recalculate();
    }

    public void AddItem(
        BrgReff brg,
        SatuanType satuan,
        int konversi,
        decimal harga,
        decimal diskon,
        decimal taxPercentage,
        string noBatch,
        DateTime tglEd,
        DeliveryInspectionType inspection,
        decimal het = 0m,
        DateTime? referenceDate = null)
    {
        var refDate = referenceDate ?? DeliveryOrderDate;
        var nextNoUrut = _listItem.Select(x => x.NoUrut).DefaultIfEmpty(0).Max() + 1;
        var item = DeliveryOrderItemType.Create(
            nextNoUrut, brg, satuan, konversi, harga, diskon, taxPercentage, noBatch, tglEd, inspection, het, refDate);
        AddItem(item, refDate);
    }

    public void AddItem(
        BrgReff brg,
        SatuanType satuan,
        int konversi,
        decimal harga,
        decimal diskon,
        decimal taxPercentage,
        string noBatch,
        DateTime tglEd,
        DeliveryInspectionType inspection,
        DateTime? referenceDate)
        => AddItem(brg, satuan, konversi, harga, diskon, taxPercentage, noBatch, tglEd, inspection, 0m, referenceDate);

    public void AddItem(
        IBrg brg,
        SatuanType satuan,
        int konversi,
        decimal harga,
        decimal diskon,
        decimal taxPercentage,
        string noBatch,
        DateTime tglEd,
        DeliveryInspectionType inspection,
        decimal het = 0m,
        DateTime? referenceDate = null)
        => AddItem(brg?.ToReff() ?? throw new ArgumentNullException(nameof(brg)), satuan, konversi, harga, diskon, taxPercentage, noBatch, tglEd, inspection, het, referenceDate);

    public void AddItem(
        IBrg brg,
        SatuanType satuan,
        int konversi,
        decimal harga,
        decimal diskon,
        decimal taxPercentage,
        string noBatch,
        DateTime tglEd,
        DeliveryInspectionType inspection,
        DateTime? referenceDate)
        => AddItem(brg?.ToReff() ?? throw new ArgumentNullException(nameof(brg)), satuan, konversi, harga, diskon, taxPercentage, noBatch, tglEd, inspection, 0m, referenceDate);

    public void RemoveItem(int noUrut)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat menghapus item pada dokumen berstatus {Status}.");

        var removed = _listItem.RemoveAll(x => x.NoUrut == noUrut);
        if (removed > 0)
        {
            var i = 1;
            foreach (var item in _listItem)
            {
                item.SetNoUrut(i++);
            }
            Recalculate();
        }
    }

    public void RemoveItem(string brgId, string noBatch)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat menghapus item pada dokumen berstatus {Status}.");

        var removed = _listItem.RemoveAll(x =>
            x.Brg.BrgId == brgId &&
            string.Equals(x.NoBatch, noBatch, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            var i = 1;
            foreach (var item in _listItem)
            {
                item.SetNoUrut(i++);
            }
            Recalculate();
        }
    }

    public void SetBiayaLain(decimal biayaLain)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat mengubah biaya lain pada dokumen berstatus {Status}.");
        if (biayaLain < 0)
            throw new ArgumentException("Biaya lain tidak boleh negatif.", nameof(biayaLain));
        BiayaLain = biayaLain;
        Recalculate();
    }

    public void SetDiskonLain(decimal diskonLain)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat mengubah diskon lain pada dokumen berstatus {Status}.");
        if (diskonLain < 0)
            throw new ArgumentException("Diskon lain tidak boleh negatif.", nameof(diskonLain));
        DiskonLain = diskonLain;
        Recalculate();
    }

    public void UpdateKeterangan(string keterangan)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat mengubah keterangan pada dokumen berstatus {Status}.");
        Keterangan = keterangan ?? string.Empty;
    }

    public void UpdateSuratJalanNo(string suratJalanNo)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat mengubah nomor surat jalan pada dokumen berstatus {Status}.");
        if (string.IsNullOrWhiteSpace(suratJalanNo))
            throw new ArgumentException("Nomor surat jalan tidak boleh kosong.", nameof(suratJalanNo));
        SuratJalanNo = suratJalanNo.Trim();
    }

    public void ClearItems()
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat mengosongkan item pada dokumen berstatus {Status}.");

        _listItem.Clear();
        Recalculate();
    }

    public void UpdateDeliveryOrderDate(DateTime deliveryOrderDate)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat mengubah tanggal dokumen pada dokumen berstatus {Status}.");

        DeliveryOrderDate = deliveryOrderDate;
    }

    public void UpdateSupplier(PartnerReff supplier)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat mengubah pemasok pada dokumen berstatus {Status}.");
        if (supplier == null || string.IsNullOrWhiteSpace(supplier.PartnerId))
            throw new ArgumentException("Supplier wajib ditentukan.", nameof(supplier));

        Supplier = supplier;
    }

    public void UpdateGudang(LayananReff gudang)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat mengubah gudang pada dokumen berstatus {Status}.");
        if (gudang == null || string.IsNullOrWhiteSpace(gudang.LayananId))
            throw new ArgumentException("Gudang tujuan wajib ditentukan.", nameof(gudang));

        Gudang = gudang;
    }

    public void UpdatePoId(string poId)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat mengubah PO pada dokumen berstatus {Status}.");

        PoId = poId?.Trim() ?? string.Empty;
    }

    public void SetPoDate(string poDate)
    {
        PoDate = poDate?.Trim() ?? string.Empty;
    }

    public void SetModified(string userId, DateTime modifiedAt)
    {
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Tidak dapat memodifikasi dokumen berstatus {Status}.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID tidak boleh kosong.", nameof(userId));

        AuditTrail.Modif(userId, modifiedAt);
    }

    public void Confirm(string userId, DateTime confirmedAt)
    {
        if (Status == DeliveryOrderStatusEnum.Confirmed)
            throw new InvalidOperationException("Dokumen sudah berstatus Confirmed.");
        if (Status == DeliveryOrderStatusEnum.Voided)
            throw new InvalidOperationException("Dokumen yang sudah dibatalkan tidak dapat dikonfirmasi.");
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Hanya dokumen berstatus Draft yang dapat dikonfirmasi. Status saat ini: {Status}.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID konfirmasi tidak boleh kosong.", nameof(userId));

        if (!_listItem.Any())
            throw new InvalidOperationException("Tidak dapat mengonfirmasi dokumen yang tidak memiliki item.");

        Status = DeliveryOrderStatusEnum.Confirmed;
        Confirmed = new AuditInfoType(userId, confirmedAt);
        AuditTrail.Modif(userId, confirmedAt);
    }

    public void Void(string userId, DateTime voidedAt)
    {
        if (Status == DeliveryOrderStatusEnum.Confirmed)
            throw new InvalidOperationException("Dokumen yang sudah dikonfirmasi bersifat immutable dan tidak dapat dibatalkan.");
        if (Status == DeliveryOrderStatusEnum.Voided)
            throw new InvalidOperationException("Dokumen sudah dibatalkan sebelumnya.");
        if (Status != DeliveryOrderStatusEnum.Draft)
            throw new InvalidOperationException($"Hanya dokumen berstatus Draft yang dapat dibatalkan. Status saat ini: {Status}.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID pembatalan tidak boleh kosong.", nameof(userId));

        Status = DeliveryOrderStatusEnum.Voided;
        AuditTrail.Batal(userId, voidedAt);
    }
}
