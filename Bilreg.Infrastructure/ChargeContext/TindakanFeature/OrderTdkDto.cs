using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public record OrderTdkDto(
    string OrderTdkId,
    DateTime OrderTdkDate,
    string PasienId,
    string PasienName,
    string RegId,
    
    string PpaId,
    string PpaName,
    
    string LayananId,
    string LayananName,
    string TarifId,
    string TarifName,
    string FreeTextOrder,
    int StatusOrder,
    string CrtUser, DateTime CrtDate, string UpdUser,
    DateTime UpdDate, string VodUser, DateTime VodDate,
    
    string TglLahir,
    string Gender)
{
    public static OrderTdkDto FromModel(OrderTdkModel model)
    {
        var result = new OrderTdkDto(
            model.OrderTdkId, model.OrderTdkDate,
            model.Pasien.PasienId, model.Pasien.PasienName,
            model.Reg.RegId,
            
            model.DokterOrder.PpaId, model.DokterOrder.PpaName, 
            model.Layanan.LayananId, model.Layanan.LayananName,
            model.Tarif.TarifId, model.Tarif.TarifName, model.FreeTextOrder,
            (int)model.StatusOrder,
            //      audit-trail
            model.AuditTrail.Created.UserId, model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId, model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId, model.AuditTrail.Voided.Timestamp,
            //
            model.Pasien.TglLahir.ToString("yyyy-MM-dd"), model.Pasien.Gender
        );
        return result;
    }

    public OrderTdkModel ToModel()
    {
        var crt = new AuditInfoType(CrtUser, CrtDate);
        var upd = new AuditInfoType(UpdUser, UpdDate);
        var vod = new AuditInfoType(VodUser, VodDate);
        var auditTrail = new AuditTrailType(crt, upd, vod);

        var pasien = new PasienReff(PasienId, PasienName, DateOnly.Parse(TglLahir), Gender);
        var reg = new RegReff(RegId, PasienId, PasienName);
        var ppa = new PpaReff(PpaId, PpaName);
        var layanan = new LayananReff(LayananId, LayananName);
        var tarif = new TarifReff(TarifId, TarifName);
        
        var result = new OrderTdkModel(OrderTdkId, OrderTdkDate, 
            pasien, reg, ppa, layanan, 
            tarif, FreeTextOrder, (StatusOrderEnum)StatusOrder, auditTrail);
        return result;
    }
}
