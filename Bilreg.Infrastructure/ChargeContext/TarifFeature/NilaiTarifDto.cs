using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public record NilaiTarifDto(string NilaiTarifId, string TarifId, string KelasId, string TipeTarifId, decimal Nilai)
{
    public static (IEnumerable<NilaiTarifDto>, IEnumerable<NilaiTarifKomponenType>) FromModel(NilaiTarifType model)
    {
        var result = model.ListVariant
            .Select(x => new NilaiTarifDto(
                $"{model.TarifId}-{x.TipeTarif.TipeTarifId}-{x.Kelas.KelasId}",
                model.TarifId,
                x.Kelas.KelasId,
                x.TipeTarif.TipeTarifId,
                x.Nilai));
        return result;
    }
    
    public NilaiTarifType ToModel(IEnumerable<NilaiTarifDto> listDto, IEnumerable<NilaiTarifKomponenDto> listKomponen)
    {
                
    }
}

public record NilaiTarifKomponenDto(string NilaiTarifId, string KomponenId, string KomponenName, decimal Nilai);
