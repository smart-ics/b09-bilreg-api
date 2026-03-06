using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record DischargeOpDto(
    string DischargeOpId,
    DateTime DischargeOpDate,
    string OrderOpId,
    string PasienId,
    string RegId,
    string KamarId,
    string PpaId,
    int PatientCondition,
    string PostOpNote,

    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate,

    string PasienName,
    string TglLahir,
    string Gender,
    string KamarName,
    string PpaName)
{
    public static DischargeOpDto FromModel(DischargeOpModel model)
    {
        var result = new DischargeOpDto(
            model.DischargeOpId,
            model.DischargeDate,
            model.OrderOp.OrderOpId,
            model.Pasien.PasienId,
            model.Reg.RegId,
            model.KamarTujuan.KamarId,
            model.Dokter.PpaId,
            (int)model.KondisiPasien,
            model.PostOpNote,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp,
            model.Pasien.PasienName,
            model.Pasien.TglLahir.ToString("yyyy-MM-dd"),
            model.Pasien.Gender,
            model.KamarTujuan.KamarName,
            model.Dokter.PpaName);
        return result;
    }

    public DischargeOpModel ToModel()
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate)
        );
        var orderOp = new OrderOpReff(OrderOpId, new DateTime(3000, 1, 1), "-");
        var dokter = new PpaReff(PpaId, PpaName);
        var pasien = new PasienReff(PasienId, PasienName, DateOnly.ParseExact(TglLahir, "yyyy-MM-dd"), Gender);
        var reg = new RegReff(RegId, PasienId, PasienName);
        var kamar = new KamarReff(KamarId, KamarName);

        var result = new DischargeOpModel(
            DischargeOpId,
            DischargeOpDate,
            auditTrail,
            orderOp,
            dokter,
            pasien,
            reg, kamar, (PatientConditionEnum)PatientCondition,
            PostOpNote);
        return result;
    }
}
