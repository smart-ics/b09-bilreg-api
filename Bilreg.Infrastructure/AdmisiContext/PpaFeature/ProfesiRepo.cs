using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public class ProfesiRepo : IProfesiRepo
{
    private readonly IProfesiDal _profesiDal;
    public ProfesiRepo(IProfesiDal profesiDal)
    {
        _profesiDal = profesiDal;
    }
    public void SaveChanges(ProfesiType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _profesiDal.Update(model),
                onNone: () => _profesiDal.Insert(model));
    }

    public MayBe<ProfesiType> LoadEntity(IProfesiKey key)
    {   
        var dto = _profesiDal.GetData(key);
        if (dto is null)
            return MayBe<ProfesiType>.None;
        var model = dto;
        return MayBe.From(model);
    }

    public void DeleteEntity(IProfesiKey key)
    {
        _profesiDal.Delete(key);
    }

    public IEnumerable<ProfesiType> ListData()
    {
        var listDto = _profesiDal.ListData()?.ToList() ?? [];
        var result = listDto;
        return result;
    }
}
