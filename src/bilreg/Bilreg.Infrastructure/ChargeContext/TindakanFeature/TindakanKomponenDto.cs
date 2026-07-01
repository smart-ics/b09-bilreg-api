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
    public static TindakanKomponenDto FromModel(TindakanKomponenBase model, string tindakanId)
    {
        var (ppaId, ppaName) = model is TindakanKomponenWithPpaType kompPpa
            ? (kompPpa.Ppa.PpaId, kompPpa.Ppa.PpaName)
            : (string.Empty, string.Empty);

        return new TindakanKomponenDto(
            tindakanId, model.NoUrut,
            model.Komponen.KomponenId, model.Komponen.KomponenName,
            ppaId, ppaName,
            model.Qty, model.Nilai, model.SubTotal);
    }
    public static TindakanKomponenDto FromModel(TindakanKomponenWithPpaType model, string tindakanId)
    {
        var result = new TindakanKomponenDto(tindakanId,  
            model.NoUrut, model.Komponen.KomponenId, model.Komponen.KomponenName,
            model.Ppa.PpaId, model.Ppa.PpaName, model.Qty, model.Nilai, model.SubTotal);
        return result;
    }

    public TindakanKomponenBase ToModel()
    {
        var komponenReff = new KomponenReff(KomponenTarifId, KomponenTarifName);
        var ppaReff = new PpaReff(PpaId, PpaName);

        TindakanKomponenBase result = PpaId == string.Empty ? 
            new TindakanKomponenWithoutPpaType(komponenReff, NoUrut, Nilai, (int)Qty, SubTotal) :
            new TindakanKomponenWithPpaType(komponenReff, ppaReff, NoUrut, Nilai, (int)Qty, SubTotal); 
        return result;
    }
}