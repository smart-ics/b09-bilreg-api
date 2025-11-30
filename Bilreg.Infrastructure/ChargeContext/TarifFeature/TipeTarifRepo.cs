using Bilreg.Application.BillContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class TipeTarifRepo : ITipeTarifRepo
{
    private readonly ITipeTarifDal _tipeTarifDal;
    public TipeTarifRepo(ITipeTarifDal tipeTarifDal)
    {
        _tipeTarifDal = tipeTarifDal;
    }
    public void SaveChanges(TipeTarifType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _tipeTarifDal.Update(TipeTarifDto.FromModel(model)),
                onNone: () => _tipeTarifDal.Insert(TipeTarifDto.FromModel(model)));
    }

    public MayBe<TipeTarifType> LoadEntity(ITipeTarifKey key)
    {   
        var dto = _tipeTarifDal.GetData(key);
        if (dto is null)
            return MayBe<TipeTarifType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(ITipeTarifKey key)
    {
        _tipeTarifDal.Delete(key);
    }

    public IEnumerable<TipeTarifType> ListData()
    {
        var listDto = _tipeTarifDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}

//  resharper disable inconsistentnaming
public record TipeTarifDto(string fs_kd_tarif_tipe, string fs_nm_tarif_tipe, bool fb_aktif, decimal fn_no_urut)
{
    public static TipeTarifDto FromModel(TipeTarifType model)
    {
        var result =  new TipeTarifDto(model.TipeTarifId, model.TipeTarifName, model.IsAktif, model.NoUrut);
        return result;
    }

    public TipeTarifType ToModel()
    {
        var result = new  TipeTarifType(fs_kd_tarif_tipe, fs_nm_tarif_tipe, fb_aktif, (int)fn_no_urut);
        return result;
    }
}
