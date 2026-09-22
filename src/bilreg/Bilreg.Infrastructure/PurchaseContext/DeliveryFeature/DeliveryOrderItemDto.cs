using Bilreg.Domain.PurchaseContext.DeliveryFeature;

namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public record DeliveryOrderItemDto(
    string DeliveryOrderId,
    int ItemNo,
    string BrgId,
    string LayananId,
    decimal QtyOrder,
    decimal QtyReceived,
    string SatuanId,
    decimal Harga,
    decimal Diskon,
    decimal Tax,
    DateTime TglEd,
    string NoBatch,
    DeliveryOrderItemStateEnum State,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static DeliveryOrderItemDto FromModel(
        DeliveryOrderModel deliveryOrder,
        DeliveryOrderItemModel item)
        => new(
            deliveryOrder.DeliveryOrderId,
            item.ItemNo,
            item.BrgId,
            item.LayananId,
            item.QtyOrder,
            item.QtyReceived,
            item.SatuanId,
            item.Harga,
            item.Diskon,
            item.Tax,
            item.TglEd,
            item.NoBatch,
            item.State,
            deliveryOrder.AuditTrail.Created.UserId,
            deliveryOrder.AuditTrail.Created.Timestamp,
            deliveryOrder.AuditTrail.Modified.UserId,
            deliveryOrder.AuditTrail.Modified.Timestamp,
            deliveryOrder.AuditTrail.Voided.UserId,
            deliveryOrder.AuditTrail.Voided.Timestamp);
}
