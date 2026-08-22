using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public record DeliveryOrderDto(
    string DeliveryOrderId,
    string DoNo,
    string SupplierId,
    string SupplierName,
    string PoReffId,
    DateTime DoDate,
    DeliveryOrderStateEnum State,
    string Notes,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static DeliveryOrderDto FromModel(DeliveryOrderModel model)
        => new(
            model.DeliveryOrderId,
            model.DoNo,
            model.Supplier.SupplierId,
            model.Supplier.SupplierName,
            model.PoReffId,
            model.DoDate,
            model.State,
            model.Notes,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp);

    public DeliveryOrderModel ToModel(IEnumerable<DeliveryOrderItemModel> listItem)
    {
        var auditTrail = AuditTrailType.Create(CrtUser, CrtDate);
        if (!string.IsNullOrWhiteSpace(UpdUser) && UpdDate.Date != EmptyDate)
            auditTrail.Modif(UpdUser, UpdDate);
        if (!string.IsNullOrWhiteSpace(VodUser) && VodDate.Date != EmptyDate)
            auditTrail.Batal(VodUser, VodDate);

        return new DeliveryOrderModel(
            DeliveryOrderId,
            DoNo,
            new SupplierReff(SupplierId, SupplierName),
            PoReffId,
            DoDate,
            State,
            Notes,
            auditTrail,
            listItem);
    }
}
