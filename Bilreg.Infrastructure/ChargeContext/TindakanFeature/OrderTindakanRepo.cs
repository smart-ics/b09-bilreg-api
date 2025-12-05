using Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public class OrderTindakanRepo : IOrderTindakanRepo
{
    private readonly IOrderTindakanDal _orderTindakanDal;

    public OrderTindakanRepo(IOrderTindakanDal orderTdkDal)
    {
        _orderTindakanDal = orderTdkDal;
    }

    public void SaveChanges(OrderTindakanModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _orderTindakanDal.Update(OrderTindakanDto.FromModel(model)), 
                onNone: () => _orderTindakanDal.Insert(OrderTindakanDto.FromModel(model)));
    }

    public MayBe<OrderTindakanModel> LoadEntity(IOrderTindakanKey key)
    {
        var dto = _orderTindakanDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }

    public void Delete(IOrderTindakanKey key)
    {
        _orderTindakanDal?.Delete(key);
    }

    public IEnumerable<OrderTindakanModel> ListData(ILayananKey lynKey)
    {
        var listDto = _orderTindakanDal.ListData(lynKey)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
