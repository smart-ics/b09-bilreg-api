namespace Bilreg.Infrastructure.BedUsageContext.WardCockpitFeature;

internal record RawBangsalRow(
    string BangsalId,
    string BangsalName,
    string KelasId,
    string KelasName,
    string LayananId = ""
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
    string? TglLahir,
    string? TglIn,
    string? JamIn,
    string? DpjpName,
    string? PenjaminName,
    string? BillingKelasName,
    string? TransaksiKelasId,
    string? RegKelasId,
    int PakaiBedPurpose,
    string? BabyName = null,
    string? BabyRegId = null,
    string? BabyMrNo = null,
    string? BabyDpjpName = null,
    string? BabyDpjpId = null,
    string? KdJenisInap = null,
    string LayananId = "",
    string KelasId = "",
    string TipeTarifId = "UM"
)
{
    public RawRoomBedRow() : this(
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
        false, null, null, null, null, null, null, null, null, null, null,
        null, null, null, 0, null, null, null, null, null, null,
        string.Empty, string.Empty, "UM") { }
}

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
    string? TglLahir,
    int ClinicalUrgency,
    int ArrivalSourceType,
    string? ReferringUnitName,
    string RequestedKelasName,
    string PlannedBedId,
    string SpecialRequirements,
    DateTime CrtDate,
    string KdJenisInap = "1",
    string RegIbu = ""
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
    string? TglLahir,
    string? TglIn,
    string? JamIn,
    decimal TarifPerHari,
    string? TipeJaminanId,
    string? PenjaminName,
    string? DokterId,
    string? DpjpName,
    string? BillingKelasName,
    string? BabyName = null,
    string? BabyRegId = null,
    string? BabyMrNo = null,
    string? BabyDpjpName = null,
    string? BabyDpjpId = null,
    string? KdJenisInap = null,
    string LayananId = "",
    string BangsalId = "",
    string KamarKelasId = "",
    string OccupancyKelasId = "",
    string KelasId = "",
    string TipeTarifId = "UM"
)
{
    public RawBedDetailRow() : this(
        string.Empty, string.Empty, string.Empty, string.Empty,
        null, null, null, null, null, null, null, null, 0m,
        null, null, null, null, null, null, null, null, null, null, null,
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, "UM") { }
}

internal record RawTindakanRow
{
    public string TindakanId { get; init; } = string.Empty;
    public DateTime TglJam { get; init; }
    public string ItemTindakan { get; init; } = string.Empty;
    public string Petugas { get; init; } = string.Empty;
    public decimal Biaya { get; init; }
    public bool HasJasaDokter { get; init; } = false;
    public string RegId { get; init; } = string.Empty;
    public bool IsBayi { get; init; } = false;

    public RawTindakanRow() { }

    public RawTindakanRow(
        string TindakanId,
        DateTime TglJam,
        string ItemTindakan,
        string Petugas,
        decimal Biaya)
    {
        this.TindakanId = TindakanId;
        this.TglJam = TglJam;
        this.ItemTindakan = ItemTindakan;
        this.Petugas = Petugas;
        this.Biaya = Biaya;
    }

    public RawTindakanRow(
        DateTime TglJam,
        string ItemTindakan,
        string Petugas,
        decimal Biaya,
        string TindakanId = "",
        bool HasJasaDokter = false,
        string RegId = "",
        bool IsBayi = false)
        : this(TindakanId, TglJam, ItemTindakan, Petugas, Biaya)
    {
        this.HasJasaDokter = HasJasaDokter;
        this.RegId = RegId;
        this.IsBayi = IsBayi;
    }
}

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
