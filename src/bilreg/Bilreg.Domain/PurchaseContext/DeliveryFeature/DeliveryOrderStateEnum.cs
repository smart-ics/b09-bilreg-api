namespace Bilreg.Domain.PurchaseContext.DeliveryFeature;

/// <summary>
/// Lifecycle of a Delivery Order (DO) document.
/// Draft -> Open -> Received -> Void
/// </summary>
public enum DeliveryOrderStateEnum
{
    Draft,     // dibuat; belum ada penerimaan
    Open,      // menerima sebagian; masih ada line yang belum diterima penuh
    Received,  // semua line sudah diterima penuh
    Void       // dibatalkan
}