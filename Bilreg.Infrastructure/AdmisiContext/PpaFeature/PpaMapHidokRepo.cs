using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;


public class PpaMapHidokRepo : IPpaMapHidokRepo
{
    private readonly IPpaMapHidokDal _ppaMapHidokDal;

    public PpaMapHidokRepo(IPpaMapHidokDal ppaMapHidokDal)
    {
        _ppaMapHidokDal = ppaMapHidokDal;
    }

    public void SaveChanges(PpaMapHidokType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _ppaMapHidokDal.Update(PpaMapHidokDto.FromModel(model)),
                onNone: () => _ppaMapHidokDal.Insert(PpaMapHidokDto.FromModel(model)));
    }

    public MayBe<PpaMapHidokType> LoadEntity(IPpaMapHidokKey key)
    {
        var dto = _ppaMapHidokDal.GetData(key);
        if (dto is null)
            return MayBe<PpaMapHidokType>.None;

        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IPpaMapHidokKey key)
    {
        _ppaMapHidokDal.Delete(key);
    }

    public IEnumerable<PpaMapHidokType> ListData()
    {
        var listDto = _ppaMapHidokDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
