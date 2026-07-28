using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.AdmisiRanapContext.OpnameRequestFeature;

public record OpnameRequestDto(
    string OpnameRequestId,
    int OpnameRequestStatus,
    string PasienId,
    string PasienName,
    string TglLahir,
    string Gender,
    string DokterId,
    string DokterName,
    DateTime PlannedDate,
    string ClinicalNotes,
    string FulfilledRegId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static OpnameRequestDto FromModel(OpnameRequestModel model) =>
        new(
            model.OpnameRequestId,
            (int)model.OpnameRequestStatus,
            model.Pasien.PasienId,
            "",
            "3000-01-01",
            "",
            model.Dokter.PpaId,
            model.Dokter.PpaName,
            model.PlannedDate,
            model.ClinicalNotes,
            model.FulfilledRegId,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp);

    public OpnameRequestModel ToModel()
    {
        var audit = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        var pasienName = string.IsNullOrWhiteSpace(PasienName) ? "-" : PasienName;
        var gender = string.IsNullOrWhiteSpace(Gender) ? "-" : Gender;
        var tglLahirRaw = string.IsNullOrWhiteSpace(TglLahir) ? "3000-01-01" : TglLahir;
        var pasien = new PasienReff(PasienId, pasienName, DateOnly.Parse(tglLahirRaw), gender);
        var dokter = new PpaReff(DokterId, DokterName);
        var insurance = OpnameRequestInsuranceModel.Default;
        return new OpnameRequestModel(
            OpnameRequestId,
            (OpnameRequestStatusEnum)OpnameRequestStatus,
            pasien,
            dokter,
            PlannedDate,
            ClinicalNotes,
            FulfilledRegId,
            audit,
            insurance);
    }
    public OpnameRequestModel ToModel(OpnameRequestInsuranceDto insuranceDto)
    {
        var audit = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        var pasienName = string.IsNullOrWhiteSpace(PasienName) ? "-" : PasienName;
        var gender = string.IsNullOrWhiteSpace(Gender) ? "-" : Gender;
        var tglLahirRaw = string.IsNullOrWhiteSpace(TglLahir) ? "3000-01-01" : TglLahir;
        var pasien = new PasienReff(PasienId, pasienName, DateOnly.Parse(tglLahirRaw), gender);
        var dokter = new PpaReff(DokterId, DokterName);
        var tipeJaminanReff = new TipeJaminanReff(insuranceDto.TipeJaminanId, insuranceDto.TipeJaminanName);
        var insurance = new OpnameRequestInsuranceModel(tipeJaminanReff, insuranceDto.ReffId);
        return new OpnameRequestModel(
            OpnameRequestId,
            (OpnameRequestStatusEnum)OpnameRequestStatus,
            pasien,
            dokter,
            PlannedDate,
            ClinicalNotes,
            FulfilledRegId,
            audit,
            insurance);
    }
}
