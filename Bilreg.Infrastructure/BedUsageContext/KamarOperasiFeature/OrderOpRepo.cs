using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class OrderOpRepo : IOrderOpRepo
{
    private readonly IOrderOpDal _orderOpDal;

    public OrderOpRepo(IOrderOpDal orderOpDal)
    {
        _orderOpDal = orderOpDal;
    }

    public void SaveChanges(OrderOpModel model)
    {
        LoadEntity(model) //  to check existence
            .Match(
                onSome: _ => _orderOpDal.Update(OrderOpDto.FromModel(model)),
                onNone: () => _orderOpDal.Insert(OrderOpDto.FromModel(model))
            );
    }

    public MayBe<OrderOpModel> LoadEntity(IOrderOpKey key)
    {
        var dto = _orderOpDal.GetData(key);
        if (dto is null)
            return MayBe<OrderOpModel>.None;

        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IOrderOpKey key)
    {
        _orderOpDal.Delete(key);
    }

    public IEnumerable<OrderOpView> ListData(Periode filter)
    {
        var listDto = _orderOpDal.ListData(filter)?.ToList() ?? [];
        var result = listDto.Select(x => new OrderOpView(x.OrderOpId, x.RegId, x.PasienId, x.PasienName, x.NamaOperasi,
            new JenisOperasiType(x.JenisOperasiId, x.fs_nm_jenis_operasi), x.PreferedDate));

        return result;
    }
}