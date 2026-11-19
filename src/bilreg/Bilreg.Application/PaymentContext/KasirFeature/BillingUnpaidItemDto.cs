namespace Bilreg.Application.PaymentContext.KasirFeature;

public record BillingUnpaidItemDto(
    string BillId,
    string RegId,
    string LayananId,
    string LayananName,
    string Keterangan,
    decimal Nilai,
    string TglTrs,
    string JamTrs)
{
    public string Id => BillId;
    public string NamaBill => string.IsNullOrWhiteSpace(Keterangan) ? LayananName : Keterangan;
    public string Layanan => LayananName;
    public string TanggalBill => TglTrs;
}
