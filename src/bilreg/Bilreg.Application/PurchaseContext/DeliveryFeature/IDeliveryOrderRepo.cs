using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature;

public interface IDeliveryOrderRepo :
    ISaveChange<DeliveryOrderModel>,
    ILoadEntity<DeliveryOrderModel, IDeliveryOrderKey>,
    IDeleteEntity<IDeliveryOrderKey>,
    IListDataMayBe<DeliveryOrderView, Periode>
{
}

public record DeliveryOrderView(
    string DeliveryOrderId,
    string SuratJalanNo,
    string PoId,
    DateTime DeliveryOrderDate,
    PartnerReff Supplier,
    LayananReff Gudang,
    string Keterangan,
    DeliveryOrderStatusEnum Status,
    decimal Total
);
