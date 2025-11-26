using Bilreg.Application.AdmisiContext.RegSub;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class RegAktifRepo : IRegAktifRepo
{
    private readonly IRegAktifDal _regAktifDal;
    public RegAktifRepo(IRegAktifDal dal)
    {
        _regAktifDal = dal;
    }
    public void SaveChanges(RegAktifModel model)
    {
        var dto = RegAktifDto.Create(model);
        var existing = _regAktifDal.GetData(model);
        if (existing is null)
            _regAktifDal.Insert(dto);
        else
            _regAktifDal.Update(dto);
    }

    public MayBe<RegAktifModel> LoadEntity(IRegKey key)
    {
        var dto = _regAktifDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }

    public void Delete(IRegKey key)
    {
        _regAktifDal.Delete(key);
    }

    public IEnumerable<RegAktifModel> ListData(Periode periode)
    {
        var listDto = _regAktifDal.ListData(periode);
        var result = listDto.Select(x => x.ToModel());  
        return result;
    }
}