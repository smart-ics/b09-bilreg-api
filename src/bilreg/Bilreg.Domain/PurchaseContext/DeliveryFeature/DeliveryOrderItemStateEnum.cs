namespace Bilreg.Domain.PurchaseContext.DeliveryFeature;

/// <summary>
/// Receipt state of a single DO line.
/// </summary>
public enum DeliveryOrderItemStateEnum
{
    Open,      // belum ada penerimaan
    Partial,   // sebagian dari QtyOrder sudah diterima
    Received   // QtyReceived == QtyOrder
}