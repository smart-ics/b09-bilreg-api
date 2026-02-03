using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.PatternHelper;

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

    public IEnumerable<RegAktifModel> ListData(ILayananKey layananKey)
    {
        var listDto = _regAktifDal.ListData(layananKey)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());  
        return result;
    }
    public IEnumerable<RegAktifModel> ListData(IPasienKey pasien)
    {
        var listDto = _regAktifDal.ListData(pasien)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());  
        return result;
    }

    public bool IsPasienAktif(IPasienKey pasien)
    {
        var listDto = _regAktifDal.ListData(pasien)?.ToList() ?? [];
        return listDto.Count != 0;
    }

    public IEnumerable<RegAktifModel> ListData()
    {
        var listDto = _regAktifDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}