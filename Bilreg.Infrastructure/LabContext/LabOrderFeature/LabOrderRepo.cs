using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public class LabOrderRepo : ILabOrderRepo
{
    private readonly ILabOrderDal _orderDal;
    private readonly ILabOrderItemDal _itemDal;
    private readonly ILabOrderItemComponentDal _itemComponentDal;

    public LabOrderRepo(
        ILabOrderDal orderDal,
        ILabOrderItemDal itemDal,
        ILabOrderItemComponentDal itemComponentDal)
    {
        _orderDal = orderDal;
        _itemDal = itemDal;
        _itemComponentDal = itemComponentDal;
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

        var listComponents = model.ItemComponents
            .Select(x => LabOrderItemComponentDto.FromModel(model.OrderId, x))
            .ToList();
        _itemComponentDal.Delete(model);
        _itemComponentDal.Insert(listComponents);
    }

    public MayBe<LabOrderModel> LoadEntity(ILabOrderKey key)
    {
        var dto = _orderDal.GetData(key);
        if (dto is null)
            return MayBe<LabOrderModel>.None;

        return MayBe.From(BuildModel(dto));
    }

    public MayBe<LabOrderModel> LoadByEmrOrderId(string emrOrderId)
    {
        var dto = _orderDal.GetByEmrOrderId(emrOrderId);
        if (dto is null)
            return MayBe<LabOrderModel>.None;

        return MayBe.From(BuildModel(dto));
    }

    public void DeleteEntity(ILabOrderKey key)
    {
        _itemComponentDal.Delete(key);
        _itemDal.Delete(key);
        _orderDal.Delete(key);
    }

    private LabOrderModel BuildModel(LabOrderDto dto)
    {
        var key = LabOrderModel.Key(dto.OrderId);
        var items = _itemDal.ListData(key)?.Select(x => x.ToModel()).ToList() ?? [];
        var components = _itemComponentDal.ListData(key)?.Select(x => x.ToModel()).ToList() ?? [];
        return dto.ToModel(items, components);
    }
}
