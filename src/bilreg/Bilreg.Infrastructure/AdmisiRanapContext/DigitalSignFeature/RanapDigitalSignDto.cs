using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.AdmisiRanapContext.DigitalSignFeature;

public record RanapDigitalSignDto(
    string SigningRequestId,
    string RegId,
    string HisReference,
    string DokumenId,
    string PasienId,
    string PasienName,
    string TglLahir,
    string Gender,
    string SignerId,
    string FileName,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static RanapDigitalSignDto FromModel(RanapDigitalSignModel model) =>
        new(
            model.SigningRequestId,
            model.RegId,
            model.HisReference,
            model.DokumenId,
            model.Pasien.PasienId,
            "",
            "3000-01-01",
            "",
            model.SignerId,
            model.FileName,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp);

    public RanapDigitalSignModel ToModel()
    {
        var audit = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        var pasienName = string.IsNullOrWhiteSpace(PasienName) ? "-" : PasienName;
        var gender = string.IsNullOrWhiteSpace(Gender) ? "-" : Gender;
        var tglLahirRaw = string.IsNullOrWhiteSpace(TglLahir) ? "3000-01-01" : TglLahir;
        var pasien = new PasienReff(PasienId, pasienName, DateOnly.Parse(tglLahirRaw), gender);
        return new RanapDigitalSignModel(
            SigningRequestId,
            RegId,
            HisReference,
            DokumenId,
            pasien,
            SignerId,
            FileName,
            audit);
    }
}
