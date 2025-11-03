using Bilreg.Application.BillContext.BedUsageFeature;
using Bilreg.Domain.BillContext.BedUsageFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BillContext.BedUsageFeature;

public class KelasRepo : IKelasRepo
{
    private readonly IKelasDal _kelasDal;

    public KelasRepo(IKelasDal kelasDal)
    {
        _kelasDal = kelasDal;
    }

    public void SaveChanges(KelasType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _kelasDal.Update(KelasDto.FromModel(model)),
                onNone: () => _kelasDal.Insert(KelasDto.FromModel(model))
            );
    }

    public MayBe<KelasType> LoadEntity(IKelasKey key)
    {
        var result = _kelasDal.GetData(key);
        var model = result?.ToModel();
        return MayBe.From(model!);
    }

    public void DeleteEntity(IKelasKey key)
    {
        _kelasDal.Delete(key);
    }

    public IEnumerable<KelasType> ListData()
    {
        var listDto = _kelasDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}