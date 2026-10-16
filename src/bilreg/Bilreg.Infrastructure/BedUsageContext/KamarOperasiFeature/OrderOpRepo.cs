using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class OrderOpRepo : IOrderOpRepo
{
    private readonly IOrderOpDal _orderOpDal;
    private readonly IOrderOpResourceDal _orderOpResourceDal;

    public OrderOpRepo(IOrderOpDal orderOpDal, IOrderOpResourceDal orderOpResourceDal)
    {
        _orderOpDal = orderOpDal;
        _orderOpResourceDal = orderOpResourceDal;
    }

    public void SaveChanges(OrderOpModel model)
    {
        using var trans = TransHelper.NewScope();

        var existing = _orderOpDal.GetData(model);
        if (existing is null)
            _orderOpDal.Insert(OrderOpDto.FromModel(model));
        else
            _orderOpDal.Update(OrderOpDto.FromModel(model));

        _orderOpResourceDal.DeleteByOrderOpId(model.OrderOpId);
        foreach (var item in model.ListResource)
        {
            _orderOpResourceDal.Insert(OrderOpResourceDto.FromModel(item));
        }

        trans.Complete();
    }

    public MayBe<OrderOpModel> LoadEntity(IOrderOpKey key)
    {
        var dto = _orderOpDal.GetData(key);
        if (dto is null)
            return MayBe<OrderOpModel>.None;

        var listResourceDto = _orderOpResourceDal.ListByOrderOpId(key.OrderOpId)?.ToList() ?? [];
        var listResource = listResourceDto.Select(x => x.ToModel()).ToList();

        var model = dto.ToModel(listResource);
        return MayBe.From(model);
    }

    public void DeleteEntity(IOrderOpKey key)
    {
        using var trans = TransHelper.NewScope();

        _orderOpResourceDal.DeleteByOrderOpId(key.OrderOpId);
        _orderOpDal.Delete(key);

        trans.Complete();
    }

    public IEnumerable<OrderOpView> ListData(Periode filter)
    {
        var listDto = _orderOpDal.ListData(filter)?.ToList() ?? [];
        var result = listDto.Select(x => new OrderOpView(
            x.OrderOpId,
            x.RegId,
            x.PasienId,
            x.PasienName,
            x.NamaOperasi,
            new JenisOperasiType(x.JenisOperasiId, x.fs_nm_jenis_operasi),
            x.PreferedDate));

        return result;
    }
}