using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class DischargeOpRepo : IDischargeOpRepo
{
    private readonly IDischargeOpDal _dischargeOpDal;

    public DischargeOpRepo(IDischargeOpDal dischargeOpDal)
    {
        _dischargeOpDal = dischargeOpDal;
    }

    public void SaveChanges(DischargeOpModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _dischargeOpDal.Update(DischargeOpDto.FromModel(model)),
                onNone: () => _dischargeOpDal.Insert(DischargeOpDto.FromModel(model)));
    }

    public MayBe<DischargeOpModel> LoadEntity(IDischargeOpKey key)
    {
        var dto = _dischargeOpDal.GetData(key);
        if (dto is null)
            return MayBe<DischargeOpModel>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IDischargeOpKey key)
    {
        _dischargeOpDal.Delete(key);
    }

    public IEnumerable<DischargeOpModel> ListData(DateTime filter)
    {
        var dto = _dischargeOpDal.ListData(filter)?.ToList() ?? [];
        return dto.Select(x => x.ToModel());
    }

}
