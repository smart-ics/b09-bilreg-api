using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;

public record AdmissionDto(
    string RegId,
    int AdmissionStatus,
    string PasienId,
    string PasienName,
    string TglLahir,
    string Gender,
    string OpnameRequestId,
    string ReservationId,
    string KelasDkId,
    string KelasDkName,
    string BangsalId,
    string BangsalName,
    DateTime AdmissionDate,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static AdmissionDto FromModel(AdmissionModel model) =>
        new(
            model.RegId,
            (int)model.AdmissionStatus,
            model.Pasien.PasienId,
            "",
            "3000-01-01",
            "",
            model.OpnameRequestId,
            model.ReservationId,
            model.KelasDk.KelasDkId,
            model.KelasDk.KelasDkName,
            model.Bangsal.BangsalId,
            model.Bangsal.BangsalName,
            model.AdmissionDate,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp);

    public AdmissionModel ToModel()
    {
        var audit = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        var pasienName = string.IsNullOrWhiteSpace(PasienName) ? "-" : PasienName;
        var gender = string.IsNullOrWhiteSpace(Gender) ? "-" : Gender;
        var tglLahirRaw = string.IsNullOrWhiteSpace(TglLahir) ? "3000-01-01" : TglLahir;
        var pasien = new PasienReff(PasienId, pasienName, DateOnly.Parse(tglLahirRaw), gender);
        var kelasDk = new KelasDkType(KelasDkId, KelasDkName);
        var bangsal = new BangsalReff(BangsalId, BangsalName);
        return new AdmissionModel(
            RegId,
            (AdmissionStatusEnum)AdmissionStatus,
            pasien,
            OpnameRequestId,
            ReservationId,
            kelasDk,
            bangsal,
            AdmissionDate,
            audit);
    }
}
