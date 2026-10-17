using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature;

public interface IDeliveryOrderRepo :
    ISaveChange<DeliveryOrderModel>,
    ILoadEntity<DeliveryOrderModel, IDeliveryOrderKey>
{
}
