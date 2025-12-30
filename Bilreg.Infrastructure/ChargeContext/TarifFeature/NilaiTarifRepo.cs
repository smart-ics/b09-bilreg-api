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

    public void Import()
    {
        //var listTrs2 = _nilaiTarifDal.ListData2()?.ToList() ?? [];
        var listTrs3 = _nilaiTarifDal.ListData3()?.ToList() ?? [];

        var listNilaiTarif = listTrs3
            .GroupBy(x => new { x.fs_kd_tarif, x.fs_kd_kelas, x.fs_kd_tipe })
            .Select(x => new NilaiTarifDto(Ulid.NewUlid().ToString(), 
                x.Key.fs_kd_tarif, x.Key.fs_kd_tipe,
                x.Key.fs_kd_kelas, x.Sum(y => y.fn_nilai), "", "", ""))
            .ToList();
        
        // var listNilaiTarifKomp = new List<NilaiTarifKompDto>();
        // foreach(var item in listNilaiTarif)
        // {
        //     var listKomp = listTrs3
        //         .Where(x => x.fs_kd_tarif == item.TarifId)
        //         .Where(x => x.fs_kd_kelas == item.KelasId)
        //         .Where(x => x.fs_kd_tipe == item.TipeTarifId)
        //         .ToList();
        //     
        //     listNilaiTarifKomp.AddRange(listKomp
        //         .Select((x, no) => new NilaiTarifKompDto(item.NilaiTarifId, no, 
        //             x.fs_kd_detil, x.fn_nilai, "")));
        // }

        var trs3Lookup = listTrs3
            .GroupBy(x => new { x.fs_kd_tarif, x.fs_kd_kelas, x.fs_kd_tipe })
            .ToDictionary(
                g => g.Key,
                g => g.ToList()
            );

        var listNilaiTarifKomp = listNilaiTarif
            .SelectMany(item =>
            {
                var key = new { 
                    fs_kd_tarif = item.TarifId, 
                    fs_kd_kelas = item.KelasId, 
                    fs_kd_tipe = item.TipeTarifId 
                };
        
                if (trs3Lookup.TryGetValue(key, out var matchingItems))
                {
                    return matchingItems.Select((x, no) => new NilaiTarifKompDto(
                        item.NilaiTarifId, 
                        no, 
                        x.fs_kd_detil, 
                        x.fn_nilai, 
                        ""));
                }
        
                return [];
            })
            .ToList();        
        
        _nilaiTarifDal.Clear();
        _nilaiTarifKompDal.Clear();
        
        _nilaiTarifDal.Insert(listNilaiTarif);
        _nilaiTarifKompDal.Insert(listNilaiTarifKomp);
    }
}

public record ta_trs_tarif2_dto(string fs_kd_trs, string fs_kd_tarif);
public record ta_trs_tarif3_dto(string fs_kd_trs, string fs_kd_tarif, string fs_kd_kelas, string fs_kd_tipe, string fs_kd_detil, decimal fn_nilai);