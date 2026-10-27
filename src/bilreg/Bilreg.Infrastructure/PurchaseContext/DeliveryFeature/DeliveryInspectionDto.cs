namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public class DeliveryInspectionDto
{
    public string DoId { get; set; } = string.Empty;
    public int NoUrut { get; set; }
    public string BrgId { get; set; } = string.Empty;
    public string BrgName { get; set; } = string.Empty;
    public string PoId { get; set; } = string.Empty;
    public int PoNoUrut { get; set; }
    public decimal QtyKirim { get; set; }
    public decimal QtyDiterima { get; set; }
    public decimal QtyDitolak { get; set; }
    public string AlasanTolak { get; set; } = string.Empty;
    public bool IsBonus { get; set; }
    public bool HasJaminanRetur { get; set; }
    public string NoSuratJaminan { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTime? ConfirmedAt { get; set; }
    public string ConfirmedBy { get; set; } = string.Empty;
    public DateTime? VoidedAt { get; set; }
    public string VoidedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedBy { get; set; } = string.Empty;
}
