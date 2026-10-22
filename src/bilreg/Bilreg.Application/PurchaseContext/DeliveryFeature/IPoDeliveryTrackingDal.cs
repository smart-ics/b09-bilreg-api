namespace Bilreg.Application.PurchaseContext.DeliveryFeature;

public class PoQuotaDto
{
    public string PoId { get; set; } = string.Empty;
    public int NoUrut { get; set; }
    public string BrgId { get; set; } = string.Empty;
    public string BrgName { get; set; } = string.Empty;
    public string SatuanId { get; set; } = string.Empty;
    public decimal QtyPo { get; set; }
    public decimal QtyDo { get; set; }
    public decimal SisaQty { get; set; }
    public bool IsClosed { get; set; }
    public string LayananId { get; set; } = string.Empty;
    public decimal Konversi { get; set; }
    public decimal Harga { get; set; }
    public decimal Diskon { get; set; }

    public PoQuotaDto() { }

    public PoQuotaDto(
        string poId,
        int noUrut,
        string brgId,
        string brgName,
        string satuanId,
        decimal qtyPo,
        decimal qtyDo,
        decimal sisaQty,
        bool isClosed,
        string layananId,
        decimal konversi,
        decimal harga,
        decimal diskon)
    {
        PoId = poId;
        NoUrut = noUrut;
        BrgId = brgId;
        BrgName = brgName;
        SatuanId = satuanId;
        QtyPo = qtyPo;
        QtyDo = qtyDo;
        SisaQty = sisaQty;
        IsClosed = isClosed;
        LayananId = layananId;
        Konversi = konversi;
        Harga = harga;
        Diskon = diskon;
    }
}

public class PoPendingItemDto
{
    public string PoId { get; set; } = string.Empty;
    public string PoDate { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string LayananId { get; set; } = string.Empty;
    public string LayananName { get; set; } = string.Empty;
    public string Keterangan { get; set; } = string.Empty;
    public decimal StatusDo { get; set; }

    public int NoUrut { get; set; }
    public string BrgId { get; set; } = string.Empty;
    public string BrgName { get; set; } = string.Empty;
    public string SatuanId { get; set; } = string.Empty;
    public string SatuanName { get; set; } = string.Empty;
    public decimal QtyPo { get; set; }
    public decimal QtyDo { get; set; }
    public decimal SisaQty { get; set; }
    public decimal Konversi { get; set; }
    public decimal Harga { get; set; }
    public decimal Diskon { get; set; }
}

public interface IPoDeliveryTrackingDal
{
    IEnumerable<PoQuotaDto> GetRemainingQuota(string poId);
    void IncrementReceivedQty(string poId, int noUrut, decimal qtyAccepted);
    void UpdatePoOverallStatus(string poId);
    IEnumerable<PoPendingItemDto> ListPendingPoItems(string? tglAwal = null, string? tglAkhir = null, string? search = null, int limit = 100);
}
