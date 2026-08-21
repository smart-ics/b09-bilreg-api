using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.PurchaseOrderFeature;

public interface IPurchaseOrderRepo :
    ISaveChange<PurchaseOrderModel>,
    ILoadEntity<PurchaseOrderModel, IPurchaseOrderKey>,
    IListDataMayBe<PurchaseOrderView, Periode>;

public record PurchaseOrderView(
    string PurchaseOrderId,
    string Keterangan,
    PartnerReff Partner,
    decimal SubTotal,
    decimal TaxTotal,
    decimal Total,
    decimal DiskonLain,
    decimal BiayaLain,
    decimal GrandTotal,
    AuditTrailType AuditTrail
);