namespace Bilreg.Application.ChargeContext.RoomChargeFeature;

public record BedOccupantItem
{
    public string PakaiBedId { get; init; } = string.Empty;
    public string RegId { get; init; } = string.Empty;
    public string PasienId { get; init; } = string.Empty;
    public string BangsalId { get; init; } = string.Empty;
    public string KamarId { get; init; } = string.Empty;
    public string BedId { get; init; } = string.Empty;
    public DateTime CheckInTime { get; init; }
    public string PhysicalClassId { get; init; } = string.Empty;
    public string PayerClassId { get; init; } = string.Empty;
    public bool IsTitipKelas { get; init; } = false;
    public bool IsNaikKelas { get; init; } = false;
    public bool IsRoomingIn { get; init; } = false;
    public string MotherPakaiBedId { get; init; } = string.Empty;
    public string TipeJaminanId { get; init; } = string.Empty;
    public decimal BaseTariff { get; init; } = 0m;
    public decimal PayerTariff { get; init; } = 0m;

    public BedOccupantItem() { }

    public BedOccupantItem(
        string PakaiBedId,
        string RegId,
        string PasienId,
        string BangsalId,
        string KamarId,
        string BedId,
        DateTime CheckInTime,
        string PhysicalClassId,
        string PayerClassId,
        bool IsTitipKelas = false,
        bool IsNaikKelas = false,
        bool IsRoomingIn = false,
        string MotherPakaiBedId = "",
        string TipeJaminanId = "",
        decimal BaseTariff = 0m,
        decimal PayerTariff = 0m)
    {
        this.PakaiBedId = PakaiBedId;
        this.RegId = RegId;
        this.PasienId = PasienId;
        this.BangsalId = BangsalId;
        this.KamarId = KamarId;
        this.BedId = BedId;
        this.CheckInTime = CheckInTime;
        this.PhysicalClassId = PhysicalClassId;
        this.PayerClassId = PayerClassId;
        this.IsTitipKelas = IsTitipKelas;
        this.IsNaikKelas = IsNaikKelas;
        this.IsRoomingIn = IsRoomingIn;
        this.MotherPakaiBedId = MotherPakaiBedId;
        this.TipeJaminanId = TipeJaminanId;
        this.BaseTariff = BaseTariff;
        this.PayerTariff = PayerTariff;
    }
}

public interface IBedOccupancyQuery
{
    IEnumerable<BedOccupantItem> GetActiveOccupants(string? bangsalId, DateTime cutOffDate);
    BedOccupantItem? GetOccupant(string pakaiBedId);
    BedOccupantItem? GetActiveOccupantByReg(string regId);
}
