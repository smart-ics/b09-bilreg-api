using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.AdmisiRanapContext.WaitingListFeature;

public record WaitingListDto(
    string WaitingListId,
    int WaitingListStatus,
    string RegId,
    string PasienId,
    string PasienName,
    string TglLahir,
    string Gender,
    string KelasId,
    string KelasName,
    string BangsalId,
    string BangsalName,
    int Priority,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static WaitingListDto FromModel(WaitingListModel model) =>
        new(
            model.WaitingListId,
            (int)model.WaitingListStatus,
            model.RegId,
            model.Pasien.PasienId,
            "",
            "3000-01-01",
            "",
            model.KelasRawat.KelasId,
            model.KelasRawat.KelasName,
            model.Bangsal.BangsalId,
            model.Bangsal.BangsalName,
            model.Priority,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp);

    public WaitingListModel ToModel()
    {
        var audit = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        var pasienName = string.IsNullOrWhiteSpace(PasienName) ? "-" : PasienName;
        var gender = string.IsNullOrWhiteSpace(Gender) ? "-" : Gender;
        var tglLahirRaw = string.IsNullOrWhiteSpace(TglLahir) ? "3000-01-01" : TglLahir;
        var pasien = new PasienReff(PasienId, pasienName, DateOnly.Parse(tglLahirRaw), gender);
        var kelasRawat = new KelasReff(KelasId, KelasName);
        var bangsal = new BangsalReff(BangsalId, BangsalName);
        return new WaitingListModel(
            WaitingListId,
            (WaitingListStatusEnum)WaitingListStatus,
            RegId,
            pasien,
            kelasRawat,
            bangsal,
            Priority,
            audit);
    }
}
