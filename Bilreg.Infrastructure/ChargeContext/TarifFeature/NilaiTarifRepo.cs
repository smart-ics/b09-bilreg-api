using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class NilaiTarifRepo : INilaiTarifRepo
{
    private readonly INilaiTarifDal _nilaiTarifDal;
    private readonly INilaiTarifKompDal _nilaiTarifKompDal;

    public NilaiTarifRepo(INilaiTarifDal nilaiTarifDal, INilaiTarifKompDal nilaiTarifKompDal)
    {
        _nilaiTarifDal = nilaiTarifDal;
        _nilaiTarifKompDal = nilaiTarifKompDal;
    }

    public void SaveChanges(NilaiTarifType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _nilaiTarifDal.Update(NilaiTarifDto.FromModel(model)),
                onNone: () => _nilaiTarifDal.Insert(NilaiTarifDto.FromModel(model)));
        
        _nilaiTarifKompDal.Delete(model);
        _nilaiTarifKompDal.Insert(model.ListKomponen.Select(x => NilaiTarifKompDto.FromModel(model.NilaiTarifId,x)));
    }

    public MayBe<NilaiTarifType> LoadEntity(INilaiTarifCompositKey compositKey)
    {   
        var listDto = _nilaiTarifDal.ListData(compositKey);

        var nilaiTarif = listDto?.Where(x => x.KelasId == compositKey.KelasId)
            .FirstOrDefault(x => x.TipeTarifId == compositKey.TipeTarifId);
        
        if (nilaiTarif is null)
            return MayBe<NilaiTarifType>.None;
        
        var listKompDto = _nilaiTarifKompDal.ListData(NilaiTarifType.Key(nilaiTarif.NilaiTarifId))?.ToList() ?? [];
        var listKomp = listKompDto.Select(x => x.ToModel());

        var model = nilaiTarif.ToModel(listKomp);
        return MayBe.From(model);
    }

    public void DeleteEntity(INilaiTarifKey key)
    {
        _nilaiTarifDal.Delete(key);
        _nilaiTarifKompDal.Delete(key);
    }

    public IEnumerable<NilaiTarifView> ListData(ILayananKey layanan, INilaiTarifVariant variant)
    {
        var listDto = _nilaiTarifDal.ListData(layanan, variant)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToView());
        return result;
    }
}