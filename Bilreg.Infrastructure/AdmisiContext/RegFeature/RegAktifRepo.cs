using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Param;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class RegAktifRepo : IRegAktifRepo
{
    private readonly IRegAktifDal _regAktifDal;
    private readonly IGetKodeRsService _getKodeRsSvc;
    public RegAktifRepo(IRegAktifDal dal,
        IGetKodeRsService getKodeRsService)
    {
        _regAktifDal = dal;
        _getKodeRsSvc = getKodeRsService;
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
        return listDto.Any();
    }

    public IEnumerable<RegAktifModel> ListData()
    {
        var listDto = _regAktifDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }

    public IEnumerable<RegSearchRegView> ListData(string keyword)
    {
        var kodeRs = _getKodeRsSvc.Execute();
        var pasienFinder = PasienFinder.CreateNew(keyword, kodeRs);
        var listRegByRegId = new List<RegSearchRegView>();
        var listRegByPasienId = new List<RegSearchRegView>();
        var listRegByName = new List<RegSearchRegView>();

        if (pasienFinder.RegId != string.Empty)
            listRegByRegId = ListRegByRegId(pasienFinder.RegId);

        if (pasienFinder.PasienId != string.Empty)
            listRegByPasienId = ListRegByPasienId(pasienFinder.PasienId);

        if (pasienFinder.StringVariants.Count > 0)
            listRegByName = ListRegByName(pasienFinder.StringVariants);

        var result = listRegByRegId
            .Union(listRegByPasienId)
            .Union(listRegByName);

        return result;
    }

    #region PRIVATE-HELPER
    private List<RegSearchRegView> ListRegByRegId(string regId)
    {
        var regDb = LoadEntity(RegModel.Key(regId));
        if (!regDb.HasValue)
            return [];

        var result = new RegSearchRegView(
            regDb.Value.RegId,
            regDb.Value.RegDate.ToString("yyyy-MM-dd"),
            regDb.Value.Pasien.PasienId,
            regDb.Value.Pasien.PasienName,
            regDb.Value.TipeJaminan.TipeJaminanName,
            regDb.Value.Layanan.LayananName,
            ((int)regDb.Value.JenisReg).ToString(),
            regDb.Value.JenisReg.ToString());
        return [result];
    }
    private List<RegSearchRegView> ListRegByPasienId(string pasienId)
    {
        var listRegDb = _regAktifDal.ListData(PasienModel.Key(pasienId));
        if (listRegDb is null)
            return [];

        var result = listRegDb.Select(x => new RegSearchRegView(
            x.RegId,
            x.RegDate.ToString("yyyy-MM-dd"),
            x.PasienId,
            x.PasienName,
            x.TipeJaminanName,
            x.LayananName,
            ((int)x.JenisReg.ToJenisRegEnum()).ToString(),
            x.JenisReg.ToJenisRegEnum().ToString()));
        return result.ToList();
    }
    private List<RegSearchRegView> ListRegByName(Dictionary<string, string[]> stringVariants)
    {
        var listRegDb = _regAktifDal.ListDataByName(stringVariants);
        if (listRegDb is null)
            return [];

        var result = listRegDb.Select(x => new RegSearchRegView(
            x.RegId,
            x.RegDate.ToString("yyyy-MM-dd"),
            x.PasienId,
            x.PasienName,
            x.TipeJaminanName,
            x.LayananName,
            ((int)x.JenisReg.ToJenisRegEnum()).ToString(),
            x.JenisReg.ToJenisRegEnum().ToString()));
        return result.ToList();
    }
    #endregion
}