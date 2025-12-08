using Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public class OrderTdkRepo : IOrderTdkRepo
{
    private readonly IOrderTdkDal _orderTdkDal;

    public OrderTdkRepo(IOrderTdkDal orderTdkDal)
    {
        _orderTdkDal = orderTdkDal;
    }

    public void SaveChanges(OrderTdkModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _orderTdkDal.Update(OrderTdkDto.FromModel(model)), 
                onNone: () => _orderTdkDal.Insert(OrderTdkDto.FromModel(model)));
    }

    public MayBe<OrderTdkModel> LoadEntity(IOrderTdkKey key)
    {
        var dto = _orderTdkDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }

    public void Delete(IOrderTdkKey key)
    {
        _orderTdkDal?.Delete(key);
    }

    public IEnumerable<OrderTdkModel> ListData(IPasienKey pasienKey)
    {
        var listDto = _orderTdkDal.ListData(pasienKey)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
