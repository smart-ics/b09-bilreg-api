using Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BillContext.TindakanSub.TindakanAgg;
using Bilreg.Infrastructure.Shared.Helpers;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public class TindakanRepo : ITindakanRepo
{
    private readonly ITindakanDal _tdkDal;
    private readonly ITindakanKomponenDal _kompDal;
    public TindakanRepo(ITindakanDal tdkDal, 
        ITindakanKomponenDal kompDal)
    {
        _tdkDal = tdkDal;
        _kompDal = kompDal;
    }

    public void SaveChanges(TindakanModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: x => _tdkDal.Update(TindakanDto.FromModel(model)),
                onNone: () => _tdkDal.Insert(TindakanDto.FromModel(model)));


        var listKomponenDto = model.Tarif.ListKomponen
            .Select(x => TindakanKomponenDto
                .FromModel(model.TindakanId, model.Tarif.Tarif.TarifId,
                x));
        _kompDal.Delete(model);
        _kompDal.Insert(listKomponenDto);
    }

    public MayBe<TindakanModel> LoadEntity(ITindakanKey key)
    {
        var data = _tdkDal.GetData(key);
        var listKomp = _kompDal.ListData(key);
        var result = data.ToModel(listKomp);
        return MayBe.From(result);
    }

    public void Delete(ITindakanKey key)
    {
        _tdkDal.Delete(key);
        _kompDal.Delete(key);
    }

    public IEnumerable<TindakanView> ListData(IRegKey regKey)
    {
        var listDto = _tdkDal.ListData(regKey);
        var result = listDto.Select(x =>  x.ToView());
        return result;
    }
}
