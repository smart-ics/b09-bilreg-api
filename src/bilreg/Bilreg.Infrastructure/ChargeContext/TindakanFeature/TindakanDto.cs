using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public record TindakanDto(
    string TindakanId, DateTime TindakanDate, string OrderTdkId,
    string RegId, string PasienId, string PasienName,
    string LayananId, string LayananName,
    string KelasId, string KelasName,
    string TipeTarifId, string TipeTarifName,
    string TarifId, string TarifName, decimal Total,
    string CrtUser, DateTime CrtDate,
    string UpdUser, DateTime UpdDate,
    string VodUser, DateTime VodDate,
    string PakaiBedId = "",
    int Qty = 1)
{
    public TindakanDto() : this("", default, "", "", "", "", "", "", "", "", "", "", "", "", 0, "", default, "", default, "", default, "", 1)
    {
    }

    public TindakanDto(
        string tindakanId, DateTime tindakanDate, string orderTdkId,
        string regId, string pakaiBedId, string pasienId, string pasienName,
        string layananId, string layananName,
        string kelasId, string kelasName,
        string tipeTarifId, string tipeTarifName,
        string tarifId, string tarifName, int qty, decimal total,
        string crtUser, DateTime crtDate,
        string updUser, DateTime updDate,
        string vodUser, DateTime vodDate)
        : this(tindakanId, tindakanDate, orderTdkId, regId, pasienId, pasienName,
            layananId, layananName, kelasId, kelasName, tipeTarifId, tipeTarifName,
            tarifId, tarifName, total, crtUser, crtDate, updUser, updDate, vodUser, vodDate,
            pakaiBedId, qty)
    {
    }

    public static TindakanDto FromModel(TindakanModel model)
    {
        var result = new TindakanDto(
            model.TindakanId, model.TindakanDate, model.OrderTindakanId, 
            model.Reg.RegId, model.Reg.PasienId, model.Reg.PasienName,
            model.Layanan.LayananId, model.Layanan.LayananName,
            model.Kelas.KelasId, model.Kelas.KelasName,
            model.TipeTarif.TipeTarifId, model.TipeTarif.TipeTarifName,
            model.Tarif.TarifId, model.Tarif.TarifName, model.Total,
            model.AuditTrail.Created.UserId, model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId, model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId, model.AuditTrail.Voided.Timestamp,
            model.PakaiBedId, model.Qty);
        return result;
    }

    public TindakanModel ToModel(IEnumerable<TindakanKomponenBase> listKomponen)
    {
        var regReff = new RegReff(RegId, PasienId, PasienName); 
        var layananReff = new LayananReff(LayananId, LayananName);

        var kelas = new KelasReff(KelasId, KelasName);
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
            kelas,
            tipeTarif,
            tarif,
            listKomponen,
            auditTrail,
            PakaiBedId,
            Qty
        );
        return result;
    }

    public TindakanView ToView()
    {
        var regReff = new RegReff(RegId, PasienId, PasienName);
        var tarifReff = new TarifReff(TarifId, TarifName);
        var lyn = new LayananReff(LayananId, LayananName);
        var result = new TindakanView(TindakanId, TindakanDate, OrderTdkId,
            regReff, lyn, tarifReff, PakaiBedId);
        return result;
    }
}

public record TindakanJualDto(
    string TransaksiId, DateTime TransaksiDate, string OrderTransaksiId,
    string RegId, string PasienId, string PasienName,
    string LayananId, string LayananName,
    string DiskripsiId, string DiskripsiName, 
    int Qty, decimal Total,
    string Tipe,
    string CrtUser, DateTime CrtDate,
    string UpdUser, DateTime UpdDate,
    string VodUser, DateTime VodDate,
    string PakaiBedId = "")
{
    public TindakanJualDto() : this("", default, "", "", "", "", "", "", "", "", 1, 0, "", "", default, "", default, "", default, "")
    {
    }

    public TindakanJualView ToView()
    {
        var regReff = new RegReff(RegId, PasienId, PasienName);
        var lyn = new LayananReff(LayananId, LayananName);

        var result = new TindakanJualView(TransaksiId, TransaksiDate, OrderTransaksiId, regReff, lyn,
            DiskripsiId, DiskripsiName, Qty, Total, Tipe, PakaiBedId);

        return result;
    }
};
