namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public class DeliveryOrderDto
{
    public string DoId { get; set; } = string.Empty;
    public DateTime DoDate { get; set; }
    public string PetugasId { get; set; } = string.Empty;
    public string PoId { get; set; } = string.Empty;
    public string PoDate { get; set; } = string.Empty;
    public string SuratJalanNo { get; set; } = string.Empty;
    public string LayananId { get; set; } = string.Empty;
    public string LayananName { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Keterangan { get; set; } = string.Empty;
    public bool PembelianBebas { get; set; }
    public string Status { get; set; } = "Confirmed";

    public DateTime TglVoid { get; set; } = new DateTime(3000, 1, 1);
    public string JamVoid { get; set; } = "00:00:00";
    public string PetugasVoid { get; set; } = string.Empty;

    public DateTime CrtTgl { get; set; } = DateTime.Now;
    public string CrtUsr { get; set; } = string.Empty;
    public DateTime UpdTgl { get; set; } = new DateTime(3000, 1, 1);
    public string UpdUsr { get; set; } = string.Empty;
}
