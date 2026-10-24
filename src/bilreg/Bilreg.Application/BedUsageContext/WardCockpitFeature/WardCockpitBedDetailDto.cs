namespace Bilreg.Application.BedUsageContext.WardCockpitFeature;

public record WardCockpitBedDetailDto(
    WardCockpitBedDetailBedInfoDto BedInfo,
    WardCockpitBedDetailOccupancyDto? Occupancy,
    IEnumerable<WardCockpitBedDetailTindakanDto> TabTindakan,
    WardCockpitBedDetailRoomChargeDto? TabRoomCharge,
    IEnumerable<WardCockpitBedDetailRiwayatMutasiDto> TabRiwayatMutasi
);

public record WardCockpitBedDetailBedInfoDto(
    string BedId,
    string BedCode,
    string RoomName,
    string KelasKamar
);

public record WardCockpitBedDetailOccupancyDto(
    string PakaiBedId,
    string RegId,
    WardCockpitBedDetailPatientDto Patient,
    WardCockpitBedDetailPenjaminDto Penjamin,
    WardCockpitBedDetailDpjpDto Dpjp,
    DateTime CheckInTime,
    int LosDays,
    string BillingKelas
);

public record WardCockpitBedDetailPatientDto(
    string MrNo,
    string NamaPasien,
    string Gender,
    string TglLahir,
    string Umur
);

public record WardCockpitBedDetailPenjaminDto(
    string TipeJaminanId,
    string TipeJaminanName,
    string NoKartu
);

public record WardCockpitBedDetailDpjpDto(
    string DokterId,
    string DokterName
);

public record WardCockpitBedDetailTindakanDto(
    DateTime TglJam,
    string ItemTindakan,
    string Petugas,
    decimal Biaya
);

public record WardCockpitBedDetailRoomChargeDto(
    decimal TarifPerHari,
    int AkumulasiHari,
    decimal EstimasiTotalSewa,
    string CutOffTime
);

public record WardCockpitBedDetailRiwayatMutasiDto(
    DateTime TglJam,
    string Kegiatan,
    string Petugas
);
