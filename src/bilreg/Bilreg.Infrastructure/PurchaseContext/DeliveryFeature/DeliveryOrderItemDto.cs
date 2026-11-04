namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public class DeliveryOrderItemDto
{
    public string DoId { get; set; } = string.Empty;
    public int NoUrut { get; set; }
    public string BrgId { get; set; } = string.Empty;
    public string BrgName { get; set; } = string.Empty;
    public decimal Harga { get; set; }
    public decimal Diskon { get; set; }
    public decimal Qty { get; set; }
    public string SatuanId { get; set; } = string.Empty;
    public string SatuanName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string NoBatch { get; set; } = string.Empty;
    public DateTime TglEd { get; set; }
    public bool IsVoid { get; set; }
    public int Konversi { get; set; }
    public decimal Het { get; set; }
    public decimal Biaya { get; set; }
}
