using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public record OrderTindakanDto(
    string OrderId,
    DateTime OrderDate,
    string PpaId,
    string PpaName,
    string RegId,
    string PasienId,
    string PasienName,
    string LayananId,
    string LayananName,
    StatusOrderEnum StatusOrder,
    string TarifId,
    string TindakanName,
    string CrtUser, DateTime CrtDate, string UpdUser,
    DateTime UpdDate, string VodUser, DateTime VodDate)
{
    public static OrderTindakanDto FromModel(OrderTindakanModel model)
    {
        var result = new OrderTindakanDto(
            OrderId: model.OrderId,
            OrderDate: model.OrderDate,
            PpaId: model.DokterOrder.PpaId,
            PpaName: model.DokterOrder.PpaName, 
            RegId: model.Reg.RegId, 
            PasienId: model.Pasien.PasienId,
            PasienName: model.Pasien.PasienName,
            LayananId: model.Layanan.LayananId,
            LayananName: model.Layanan.LayananName,
            StatusOrder: model.StatusOrder,
            TarifId: model.Tindakan.TarifId,
            TindakanName: model.Tindakan.TarifName,
            //      audit-trail
            model.AuditTrail.Created.UserId, model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId, model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId, model.AuditTrail.Voided.Timestamp
        );
        return result;
    }

    public OrderTindakanModel ToModel()
    {
        var crt = new AuditInfoType(CrtUser, CrtDate);
        var upd = new AuditInfoType(UpdUser, UpdDate);
        var vod = new AuditInfoType(VodUser, VodDate);
        var auditTrail = new AuditTrailType(crt, upd, vod);

        var pasien = new PasienReff(PasienId, PasienName, new DateOnly(3000,1,1), "X");
        var reg = new RegReff(RegId, PasienId, PasienName);
        var ppa = new PpaReff(PpaId, PpaName);
        var layanan = new LayananReff(LayananId, LayananName);
        var tarif = new TarifReff(TarifId, TindakanName);
        
        var result = new OrderTindakanModel(OrderId, OrderDate, pasien, reg, ppa, layanan, 
            StatusOrder, tarif, auditTrail);
        return result;
    }
}
