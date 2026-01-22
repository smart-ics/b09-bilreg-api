using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public class TindakanRepo : ITindakanRepo
{
    private readonly ITindakanDal _tindakanDal;
    private readonly ITindakanKomponenDal _tindakanKompDal;
    public TindakanRepo(ITindakanDal tindakanDal, ITindakanKomponenDal tindakanKompDal)
    {
        _tindakanDal = tindakanDal;
        _tindakanKompDal = tindakanKompDal;
    }

    public void SaveChanges(TindakanModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: x => _tindakanDal.Update(TindakanDto.FromModel(model)),
                onNone: () => _tindakanDal.Insert(TindakanDto.FromModel(model)));


        var listKomponenDto = model.ListKomponen
            .Select(x => TindakanKomponenDto.FromModel(x, model.TindakanId));
        _tindakanKompDal.Delete(model);
        _tindakanKompDal.Insert(listKomponenDto);
    }

    public MayBe<TindakanModel> LoadEntity(ITindakanKey key)
    {
        var data = _tindakanDal.GetData(key);
        if (data is null)
            return MayBe<TindakanModel>.None;

        var listKompDto = _tindakanKompDal.ListData(key)?.ToList() ?? [];
        var result = data.ToModel(listKompDto.Select(x => x.ToModel()));

        return MayBe.From(result);
    }

    public void Delete(ITindakanKey key)
    {
        _tindakanDal.Delete(key);
        _tindakanKompDal.Delete(key);
    }

    public IEnumerable<TindakanView> ListData(IRegKey regKey)
    {
        var listDto = _tindakanDal.ListData(regKey);
        var result = listDto.Select(x =>  x.ToView());
        return result;
    }

    public IEnumerable<TindakanJualView> ListDataTdkJual(IRegKey regKey)
    {
        var listDto = _tindakanDal.ListTdkJual(regKey)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToView())?.ToList() ?? [];
        return result;
    }
}
