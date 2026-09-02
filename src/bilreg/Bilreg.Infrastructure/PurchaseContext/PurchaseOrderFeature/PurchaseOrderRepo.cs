using Bilreg.Application.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PurchaseContext.PurchaseOrderFeature;

public class PurchaseOrderRepo : IPurchaseOrderRepo
{
    private readonly IPurchaseOrderDal _purchaseOrderDal;
    private readonly IPurchaseOrderItemDal _purchaseOrderItemDal;

    public PurchaseOrderRepo(IPurchaseOrderDal purchaseOrderDal, IPurchaseOrderItemDal purchaseOrderItemDal)
    {
        _purchaseOrderDal = purchaseOrderDal;
        _purchaseOrderItemDal = purchaseOrderItemDal;
    }

    public void SaveChanges(PurchaseOrderModel model)
    {
        var dto = PurchaseOrderDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _purchaseOrderDal.Update(dto),
                onNone: () => _purchaseOrderDal.Insert(dto)
            );

        var listItemDto = model.ListItem.Select(x => PurchaseOrderItemDto.FromModel(model.PurchaseOrderId, x));
        _purchaseOrderItemDal.Delete(model);
        _purchaseOrderItemDal.Insert(listItemDto);
    }

    public MayBe<PurchaseOrderModel> LoadEntity(IPurchaseOrderKey key)
    {
        var dto = _purchaseOrderDal.GetData(key);
        if (dto is null)
            return MayBe<PurchaseOrderModel>.None;
        
        var listItemDto = _purchaseOrderItemDal.ListData(key)?.ToList() ?? [];
        var listItem = listItemDto.Select(x => x.ToModel());
        var model = dto.ToModel(listItem);
        return MayBe.From(model);
    }

    public MayBe<IEnumerable<PurchaseOrderView>> ListData(Periode filter)
    {
        var listDto = _purchaseOrderDal.ListData(filter)?.ToList() ?? [];
        var listPurchaseOrder = listDto.Select(x => x.ToView());
        return MayBe.From(listPurchaseOrder);
    }
}