namespace Bilreg.Application.BedUsageContext.WardCockpitFeature;

public record WardCockpitOverviewDto(
    WardCockpitBangsalDto Bangsal,
    WardCockpitKpiDto Kpi,
    IEnumerable<WardCockpitRoomDto> Rooms,
    IEnumerable<WardCockpitWaitingListDto> WaitingList
);

public record WardCockpitBangsalDto(
    string BangsalId,
    string BangsalName,
    string KelasId,
    string KelasName
);

public record WardCockpitKpiDto(
    int TotalBed,
    int Occupied,
    double BorPercentage,
    int Available,
    int Cleaning,
    int Maintenance,
    int WaitingListCount
);

public record WardCockpitRoomDto(
    string RoomId,
    string RoomName,
    string KelasKamar,
    string GenderRestriction,
    IEnumerable<WardCockpitBedDto> Beds
);

public record WardCockpitBedDto(
    string BedId,
    string BedCode,
    string Status,
    WardCockpitOccupancyDto? CurrentOccupancy,
    WardCockpitOperationalStateDto OperationalState
);

public record WardCockpitOccupancyDto(
    string PakaiBedId,
    string RegId,
    string MrNo,
    string NamaPasien,
    string Gender,
    string Umur,
    DateTime TglJamMasuk,
    int LosDays,
    string DpjpName,
    string PenjaminName,
    string BillingKelasName,
    bool IsTitipKelas,
    bool IsRawatGabung
);

public record WardCockpitOperationalStateDto(
    string ReadinessStatus,
    string BlockerType,
    DateTime? DirtySince,
    string? MaintenanceNote
);

public record WardCockpitWaitingListDto(
    string WaitingListId,
    string RegId,
    string MrNo,
    string NamaPasien,
    string Gender,
    string Umur,
    string ClinicalUrgency,
    string ReferralSource,
    string RequestedKelasName,
    string PlannedBedId,
    int ElapsedWaitMinutes,
    string SpecialRequirements
);
