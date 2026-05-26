using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public record NilaiTarifDto(string NilaiTarifId, string TarifId, string TipeTarifId, 
    string KelasId, decimal Nilai, string TarifName, string TipeTarifName, string KelasName,
    string SourcePolicyId)
{
    public static NilaiTarifDto FromModel(NilaiTarifType model, string sourcePolicyId = "")
    {
        var result = new NilaiTarifDto(
            model.NilaiTarifId, 
            model.TarifId, 
            model.TipeTarif.TipeTarifId, 
            model.Kelas.KelasId, 
            model.Nilai, 
            model.TarifName, 
            model.TipeTarif.TipeTarifName, 
            model.Kelas.KelasName,
            sourcePolicyId);
        return result;
    }

    public NilaiTarifType ToModel(IEnumerable<NilaiTarifKomponenType> listKomponen)
    {
        var tipeTarif = new TipeTarifReff(TipeTarifId, TipeTarifName);
        var kelas = new KelasReff(KelasId, KelasName);
        var result = new NilaiTarifType(NilaiTarifId, TarifId, TarifName, tipeTarif, kelas, Nilai, listKomponen);
        return result;
 }

    public NilaiTarifView ToView()
    {
        var tipeTarifReff = new TipeTarifReff(TipeTarifId, TipeTarifName);
        var kelasReff = new KelasReff(KelasId, KelasName);
        return new NilaiTarifView(TarifId, TarifName, tipeTarifReff, kelasReff, Nilai);
    }
}
