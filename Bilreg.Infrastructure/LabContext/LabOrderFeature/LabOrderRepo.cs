using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public class LabOrderRepo : ILabOrderRepo
{
    private readonly ILabOrderDal _orderDal;
    private readonly ILabOrderItemDal _itemDal;

    public LabOrderRepo(ILabOrderDal orderDal, ILabOrderItemDal itemDal)
    {
        _orderDal = orderDal;
        _itemDal = itemDal;
    }

    public void SaveChanges(LabOrderModel model)
    {
        var dto = LabOrderDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _orderDal.Update(dto),
                onNone: () => _orderDal.Insert(dto));

        var listItems = model.Items.Select(x => LabOrderItemDto.FromModel(model.OrderId, x)).ToList();
        _itemDal.Delete(model);
        _itemDal.Insert(listItems);
    }

    public MayBe<LabOrderModel> LoadEntity(ILabOrderKey key)
    {
        var dto = _orderDal.GetData(key);
        if (dto is null)
            return MayBe<LabOrderModel>.None;

        var items = _itemDal.ListData(key)?.Select(x => x.ToModel()).ToList() ?? [];
        return MayBe.From(dto.ToModel(items));
    }

    public void DeleteEntity(ILabOrderKey key)
    {
        _itemDal.Delete(key);
        _orderDal.Delete(key);
    }
}
