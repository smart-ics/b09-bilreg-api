namespace Bilreg.Application.PaymentContext.KasirFeature;

public class BillingUnpaidItemDto
{
    public string BillId { get; set; } = string.Empty;
    public string RegId { get; set; } = string.Empty;
    public string LayananId { get; set; } = string.Empty;
    public string LayananName { get; set; } = string.Empty;
    public string Keterangan { get; set; } = string.Empty;
    public decimal Nilai { get; set; }
    public string TglTrs { get; set; } = string.Empty;
    public string JamTrs { get; set; } = string.Empty;

    public string Id => BillId;
    public string NamaBill => string.IsNullOrWhiteSpace(Keterangan) ? LayananName : Keterangan;
    public string Layanan => LayananName;
    public string TanggalBill => TglTrs;

    public BillingUnpaidItemDto() { }

    public BillingUnpaidItemDto(
        string billId,
        string regId,
        string layananId,
        string layananName,
        string keterangan,
        decimal nilai,
        string tglTrs,
        string jamTrs)
    {
        BillId = billId;
        RegId = regId;
        LayananId = layananId;
        LayananName = layananName;
        Keterangan = keterangan;
        Nilai = nilai;
        TglTrs = tglTrs;
        JamTrs = jamTrs;
    }
}
