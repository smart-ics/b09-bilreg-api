using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class Icd10Repo : IIcd10Repo
{
    private readonly IIcd10Dal _icd10Dal;

    public Icd10Repo(IIcd10Dal icd10Dal)
    {
        _icd10Dal = icd10Dal;
    }

    public void SaveChanges(Icd10Type model)
    {
        LoadEntity(model) //  to check existence
            .Match(
                onSome: _ => _icd10Dal.Update(Icd10Dto.FromModel(model)),
                onNone: () => _icd10Dal.Insert(Icd10Dto.FromModel(model))
            );
    }

    public MayBe<Icd10Type> LoadEntity(IIcd10Key key)
    {
        var dto = _icd10Dal.GetData(key);
        if (dto is null)
            return MayBe<Icd10Type>.None;

        var model = dto.ToModel();
        return MayBe.From(model);
    }
}
