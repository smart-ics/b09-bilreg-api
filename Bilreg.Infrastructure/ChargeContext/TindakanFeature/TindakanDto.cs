using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public record TindakanDto(
    string TindakanId, DateTime TindakanDate, string OrderTdkId,
    string RegId, string PasienId, string PasienName,
    string LayananId, string LayananName,
    string TipeTarifId, string TipeTarifName,
    string TarifId, string TarifName, decimal Total,
    string CrtUser, DateTime CrtDate,
    string UpdUser, DateTime UpdDate,
    string VodUser, DateTime VodDate)
{
    public static TindakanDto FromModel(TindakanModel model)
    {
        var result = new TindakanDto(
            model.TindakanId, model.TindakanDate, model.OrderTindakanId, 
            model.Reg.RegId, model.Reg.PasienId, model.Reg.PasienName,
            model.Layanan.LayananId, model.Layanan.LayananName,
            model.TipeTarif.TipeTarifId, model.TipeTarif.TipeTarifName,
            model.Tarif.TarifId, model.Tarif.TarifName, model.Total,
            model.AuditTrail.Created.UserId, model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId, model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId, model.AuditTrail.Voided.Timestamp);
        return result;
    }

    public TindakanModel ToModel(IEnumerable<TindakanKomponenType> listKomponen)
    {
        var regReff = new RegReff(RegId, PasienId, PasienName); 
        var layananReff = new LayananReff(LayananId, LayananName);

        var tipeTarif = new TipeTarifReff(TipeTarifId, TipeTarifName);
        var tarif = new TarifReff(TarifId, TarifName);

        var crt = new AuditInfoType(CrtUser, CrtDate);
        var upd = new AuditInfoType(UpdUser, UpdDate);
        var vod = new AuditInfoType(VodUser, VodDate);
        var auditTrail = new AuditTrailType(crt, upd, vod);
        
        var result = new TindakanModel(
            TindakanId, TindakanDate, OrderTdkId, 
            regReff,
            layananReff,
            tipeTarif,
            tarif,
            listKomponen,
            auditTrail
        );
        return result;
    }

    public TindakanView ToView()
    {
        var regReff = new RegReff(RegId, PasienId, PasienName);
        var tarifReff = new TarifReff(TarifId, TarifName);
        var lyn = new LayananReff(LayananId, LayananName);
        var result = new TindakanView(TindakanId, TindakanDate, OrderTdkId,
            regReff, lyn, tarifReff);
        return result;
    }
}

public record TindakanKomponenDto(
    string TindakanId, string TarifId, int NoUrut, 
    string KomponenTarifId, string KomponenTarifName,
    string PpaId, string PpaName,
    decimal Qty, decimal Nilai, decimal SubTotal)
{
    public static TindakanKomponenDto FromModel(TindakanKomponenType model, TindakanModel hdr)
    {
        var result = new TindakanKomponenDto(hdr.TindakanId, hdr.Tarif.TarifId, 
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
