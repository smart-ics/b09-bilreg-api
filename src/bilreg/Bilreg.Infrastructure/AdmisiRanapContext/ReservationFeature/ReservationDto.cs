using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.AdmisiRanapContext.ReservationFeature;

public record ReservationDto(
    string ReservationId,
    int ReservationStatus,
    string PasienId,
    string PasienName,
    DateTime TglLahir,
    string Gender,
    string OpnameRequestId,
    DateTime PlannedDate,
    string KelasId,
    string KelasName,
    string BangsalId,
    string BangsalName,
    string RealizedRegId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static ReservationDto FromModel(ReservationModel model) =>
        new(
            model.ReservationId,
            (int)model.ReservationStatus,
            model.Pasien.PasienId,
            model.Pasien.PasienName,
            model.Pasien.TglLahir.ToDateTime(TimeOnly.MinValue),
            model.Pasien.Gender,
            model.OpnameRequestId,
            model.PlannedDate,
            model.KelasRawat.KelasId,
            model.KelasRawat.KelasName,
            model.Bangsal.BangsalId,
            model.Bangsal.BangsalName,
            model.RealizedRegId,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp);

    public ReservationModel ToModel()
    {
        var audit = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        var pasien = new PasienReff(PasienId, PasienName, DateOnly.FromDateTime(TglLahir), Gender);
        var kelasRawat = new KelasReff(KelasId, KelasName);
        var bangsal = new BangsalReff(BangsalId, BangsalName);
        return new ReservationModel(
            ReservationId,
            (ReservationStatusEnum)ReservationStatus,
            pasien,
            OpnameRequestId,
            PlannedDate,
            kelasRawat,
            bangsal,
            RealizedRegId,
            audit);
    }
}
