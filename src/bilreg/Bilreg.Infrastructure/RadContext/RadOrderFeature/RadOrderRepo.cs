using System.Transactions;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.RadContext.RadOrderFeature;

public class RadOrderRepo : IRadOrderRepo
{
    private readonly IRadOrderTableDal _orderDal;
    private readonly IRadOrderItemDal _itemDal;

    public RadOrderRepo(IRadOrderTableDal orderDal, IRadOrderItemDal itemDal)
    {
        _orderDal = orderDal;
        _itemDal = itemDal;
    }

    public void SaveChanges(RadOrderModel model)
    {
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var dto = RadOrderDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _orderDal.Update(dto),
                onNone: () => _orderDal.Insert(dto));

        var listItems = model.Items
            .Select(x => RadOrderItemDto.FromModel(model.OrderId, x))
            .ToList();
        _itemDal.Delete(model);
        _itemDal.Insert(listItems);

        scope.Complete();
    }

    public MayBe<RadOrderModel> LoadEntity(IRadOrderKey key)
    {
        var dto = _orderDal.GetData(key);
        if (dto is null)
            return MayBe<RadOrderModel>.None;

        return MayBe.From(BuildModel(dto));
    }

    public MayBe<RadOrderModel> LoadByEmrOrderId(string emrOrderId)
    {
        var dto = _orderDal.GetByEmrOrderId(emrOrderId);
        if (dto is null)
            return MayBe<RadOrderModel>.None;

        return MayBe.From(BuildModel(dto));
    }

    public IEnumerable<RadOrderModel> ListByRegId(string regId)
    {
        var dtoList = _orderDal.ListByRegId(regId);
        return dtoList?.Select(BuildModel).ToList() ?? [];
    }

    private RadOrderModel BuildModel(RadOrderDto dto)
    {
        var items = _itemDal.ListData(dto)?.Select(x => x.ToModel()).ToList() ?? [];
        return dto.ToModel(items);
    }
}
