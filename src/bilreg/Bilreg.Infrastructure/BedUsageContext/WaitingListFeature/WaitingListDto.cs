using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.BedUsageContext.WaitingListFeature;

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
    int ArrivalSourceType,
    string ReferringUnitId,
    string ReferringUnitName,
    string ReferringDoctorId,
    string ReferringDoctorName,
    int ClinicalUrgency,
    string SpecialRequirements,
    string PlannedBedId,
    string PlannedBedName,
    string PlacedBedId,
    string PlacedPakaiBedId,
    string CancellationReason,
    string RerouteReason,
    string PreviousWaitListId,
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
            model.Pasien?.PasienId ?? string.Empty,
            model.Pasien?.PasienName ?? string.Empty,
            model.Pasien?.TglLahir.ToString("yyyy-MM-dd") ?? "3000-01-01",
            model.Pasien?.Gender ?? string.Empty,
            model.KelasRawat?.KelasId ?? string.Empty,
            model.KelasRawat?.KelasName ?? string.Empty,
            model.Bangsal?.BangsalId ?? string.Empty,
            model.Bangsal?.BangsalName ?? string.Empty,
            (int)model.ArrivalSourceType,
            model.ReferringUnit?.LayananId ?? string.Empty,
            model.ReferringUnit?.LayananName ?? string.Empty,
            model.ReferringDoctor?.DokterId ?? string.Empty,
            model.ReferringDoctor?.DokterName ?? string.Empty,
            (int)model.ClinicalUrgency,
            model.SpecialRequirements ?? string.Empty,
            model.PlannedBed?.BedId ?? string.Empty,
            model.PlannedBed?.BedName ?? string.Empty,
            model.PlacedBedId ?? string.Empty,
            model.PlacedPakaiBedId ?? string.Empty,
            model.CancellationReason ?? string.Empty,
            model.RerouteReason ?? string.Empty,
            model.PreviousWaitListId ?? string.Empty,
            model.Priority,
            model.AuditTrail?.Created?.UserId ?? string.Empty,
            model.AuditTrail?.Created?.Timestamp ?? new DateTime(3000, 1, 1),
            model.AuditTrail?.Modified?.UserId ?? string.Empty,
            model.AuditTrail?.Modified?.Timestamp ?? new DateTime(3000, 1, 1),
            model.AuditTrail?.Voided?.UserId ?? string.Empty,
            model.AuditTrail?.Voided?.Timestamp ?? new DateTime(3000, 1, 1));

    public WaitingListModel ToModel()
    {
        var audit = new AuditTrailType(
            new AuditInfoType(CrtUser ?? string.Empty, CrtDate),
            new AuditInfoType(UpdUser ?? string.Empty, UpdDate),
            new AuditInfoType(VodUser ?? string.Empty, VodDate));

        var pasienName = string.IsNullOrWhiteSpace(PasienName) ? "-" : PasienName;
        var gender = string.IsNullOrWhiteSpace(Gender) ? "-" : Gender;
        var tglLahirRaw = string.IsNullOrWhiteSpace(TglLahir) ? "3000-01-01" : TglLahir;
        var tglLahir = DateOnly.TryParse(tglLahirRaw, out var dt) ? dt : new DateOnly(3000, 1, 1);
        var pasien = new PasienReff(PasienId ?? string.Empty, pasienName, tglLahir, gender);
        var kelasRawat = new KelasReff(KelasId ?? string.Empty, KelasName ?? string.Empty);
        var bangsal = new BangsalReff(BangsalId ?? string.Empty, BangsalName ?? string.Empty);
        var referringUnit = new LayananReff(ReferringUnitId ?? string.Empty, ReferringUnitName ?? string.Empty);
        var referringDoctor = new DokterReff(ReferringDoctorId ?? string.Empty, ReferringDoctorName ?? string.Empty);
        var plannedBed = new BedReff(PlannedBedId ?? string.Empty, PlannedBedName ?? string.Empty, false);

        return new WaitingListModel(
            WaitingListId ?? string.Empty,
            (WaitingListStatusEnum)WaitingListStatus,
            RegId ?? string.Empty,
            pasien,
            kelasRawat,
            bangsal,
            (ArrivalSourceTypeEnum)ArrivalSourceType,
            referringUnit,
            referringDoctor,
            (ClinicalUrgencyEnum)ClinicalUrgency,
            SpecialRequirements ?? string.Empty,
            plannedBed,
            CancellationReason ?? string.Empty,
            RerouteReason ?? string.Empty,
            PreviousWaitListId ?? string.Empty,
            PlacedBedId ?? string.Empty,
            PlacedPakaiBedId ?? string.Empty,
            Priority,
            audit);
    }
}
