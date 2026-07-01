using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public record NilaiTarifKompDto(string NilaiTarifId, int NoUrut, string KomponenId, 
    decimal Nilai, string KomponenName)
{
    public static NilaiTarifKompDto FromModel(string nilaiTarifId, NilaiTarifKomponenType model)
    {
        var result = new NilaiTarifKompDto(
            nilaiTarifId, 
            model.NoUrut, 
            model.Komponen.KomponenId, 
            model.Nilai, 
            model.Komponen.KomponenName);
        return result;
    }

    public NilaiTarifKomponenType ToModel()
    {
        var komponen = new KomponenReff(KomponenId, KomponenName);
        var result = new NilaiTarifKomponenType(NoUrut, komponen, Nilai);
        return result;
    }
}