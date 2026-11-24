using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;

public record AdmissionDto(
    string RegId,
    int AdmissionStatus,
    int AdmissionSource,
    string PasienId,
    string PasienName,
    string TglLahir,
    string Gender,
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
    DateTime VodDate,
    string InpatientOrderId = "-",
    string JenisInapId = "",
    string JenisInapName = "")
{
    public static AdmissionDto FromModel(AdmissionModel model) =>
        new(
            model.RegId,
            (int)model.AdmissionStatus,
            (int)model.AdmissionSource,
            model.Pasien.PasienId,
            "",
            "3000-01-01",
            "",
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
            model.AuditTrail.Voided.Timestamp,
            model.InpatientOrderId,
            model.JenisInap.JenisInapId,
            model.JenisInap.JenisInapName);

    public AdmissionModel ToModel()
    {
        var audit = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        var pasienName = string.IsNullOrWhiteSpace(PasienName) ? "-" : PasienName;
        var gender = string.IsNullOrWhiteSpace(Gender) ? "-" : Gender;
        var tglLahirRaw = string.IsNullOrWhiteSpace(TglLahir) ? "3000-01-01" : TglLahir;
        DateOnly parsedTglLahir;
        if (DateTime.TryParse(tglLahirRaw, out var dt))
            parsedTglLahir = DateOnly.FromDateTime(dt);
        else if (!DateOnly.TryParse(tglLahirRaw, out parsedTglLahir))
            parsedTglLahir = new DateOnly(3000, 1, 1);
        var pasien = new PasienReff(PasienId, pasienName, parsedTglLahir, gender);
        var kelasDk = new KelasDkType(KelasDkId, KelasDkName);
        var bangsal = new BangsalReff(BangsalId, BangsalName);
        var jenisInapId = (JenisInapId ?? string.Empty).Trim();
        var jenisInapName = (JenisInapName ?? string.Empty).Trim();
        var jenisInap = string.IsNullOrWhiteSpace(jenisInapId)
            ? JenisInapType.Empty
            : new JenisInapType(
                jenisInapId,
                string.IsNullOrWhiteSpace(jenisInapName)
                    ? JenisInapType.FromId(jenisInapId).JenisInapName
                    : jenisInapName);
        return new AdmissionModel(
            RegId,
            (AdmissionStatusEnum)AdmissionStatus,
            pasien,
            "-",
            "-",
            kelasDk,
            bangsal,
            AdmissionDate,
            audit,
            (AdmissionSourceEnum)AdmissionSource,
            InpatientOrderId,
            jenisInap);
    }
}
