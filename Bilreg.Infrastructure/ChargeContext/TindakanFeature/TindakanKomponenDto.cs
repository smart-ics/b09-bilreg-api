using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public record TindakanKomponenDto(
    string TindakanId, int NoUrut, 
    string KomponenTarifId, string KomponenTarifName,
    string PpaId, string PpaName,
    decimal Qty, decimal Nilai, decimal SubTotal)
{
    public static TindakanKomponenDto FromModel(TindakanKomponenType model, string tindakanId)
    {
        var result = new TindakanKomponenDto(tindakanId,  
            model.NoUrut, model.Komponen.KomponenId, model.Komponen.KomponenName,
            model.Ppa.PpaId, model.Ppa.PpaName, model.Qty, model.Nilai, model.SubTotal);
        return result;
    }

    public TindakanKomponenType ToModel()
    {
        var komponenReff = new KomponenReff(KomponenTarifId, KomponenTarifName);
        var ppaReff = new PpaReff(PpaId, PpaName);

        var result = new TindakanKomponenType(komponenReff, ppaReff,
            NoUrut, Nilai, Qty, SubTotal); 
        return result;
    }
}