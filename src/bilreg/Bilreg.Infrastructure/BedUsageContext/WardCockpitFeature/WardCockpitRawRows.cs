namespace Bilreg.Infrastructure.BedUsageContext.WardCockpitFeature;

internal record RawBangsalRow(
    string BangsalId,
    string BangsalName,
    string KelasId,
    string KelasName
);

internal record RawRoomBedRow(
    string RoomId,
    string RoomName,
    string KelasKamar,
    string BedId,
    string BedCode,
    bool IsBedActive,
    string? PakaiBedId,
    string? RegId,
    string? MrNo,
    string? NamaPasien,
    string? Gender,
    DateTime? TglLahir,
    string? TglIn,
    string? JamIn,
    string? DpjpName,
    string? PenjaminName,
    string? BillingKelasName,
    string? TransaksiKelasId,
    string? RegKelasId,
    int PakaiBedPurpose
);

internal record RawBedOperationalRow(
    string BedId,
    int? CurrentReadiness,
    bool IsBlocked,
    int CurrentRestrictionType,
    string? CurrentBlockerReason,
    DateTime UpdDate
);

internal record RawWaitingListRow(
    string WaitingListId,
    string RegId,
    string MrNo,
    string NamaPasien,
    string Gender,
    DateTime? TglLahir,
    int ClinicalUrgency,
    int ArrivalSourceType,
    string? ReferringUnitName,
    string RequestedKelasName,
    string PlannedBedId,
    string SpecialRequirements,
    DateTime CrtDate
);

internal record RawBedDetailRow(
    string BedId,
    string BedCode,
    string RoomName,
    string KelasKamar,
    string? PakaiBedId,
    string? RegId,
    string? MrNo,
    string? NamaPasien,
    string? Gender,
    DateTime? TglLahir,
    string? TglIn,
    string? JamIn,
    decimal TarifPerHari,
    string? TipeJaminanId,
    string? PenjaminName,
    string? DokterId,
    string? DpjpName,
    string? BillingKelasName
);

internal record RawTindakanRow(
    DateTime TglJam,
    string ItemTindakan,
    string Petugas,
    decimal Biaya
);

internal record RawMutasiRow(
    string TglIn,
    string JamIn,
    string? TglOut,
    string? JamOut,
    string PetugasIn,
    string? PetugasOut,
    string RoomName,
    string BedCode
);
