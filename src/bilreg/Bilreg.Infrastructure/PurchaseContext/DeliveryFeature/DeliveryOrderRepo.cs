using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public class DeliveryOrderRepo : IDeliveryOrderRepo
{
    private readonly IDeliveryOrderDal _deliveryOrderDal;
    private readonly IDeliveryOrderItemDal _deliveryOrderItemDal;

    public DeliveryOrderRepo(
        IDeliveryOrderDal deliveryOrderDal,
        IDeliveryOrderItemDal deliveryOrderItemDal)
    {
        _deliveryOrderDal = deliveryOrderDal;
        _deliveryOrderItemDal = deliveryOrderItemDal;
    }

    public void SaveChanges(DeliveryOrderModel model)
    {
        var dto = DeliveryOrderDto.FromModel(model);
        var existing = _deliveryOrderDal.GetData(model);
        if (existing is null)
            _deliveryOrderDal.Insert(dto);
        else
            _deliveryOrderDal.Update(dto);

        var listItemDto = model.ListItem
            .Select(item => DeliveryOrderItemDto.FromModel(model, item));
        _deliveryOrderItemDal.Delete(model);
        _deliveryOrderItemDal.Insert(listItemDto);
    }

    public MayBe<DeliveryOrderModel> LoadEntity(IDeliveryOrderKey key)
        => throw new NotSupportedException(
            "DeliveryOrderModel loading is deferred until DeliveryOrderItemModel supports rehydrating QtyReceived and State.");
}
