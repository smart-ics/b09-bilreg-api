namespace Bilreg.Application.ChargeContext.RoomChargeFeature;

public record BedOccupantItem(
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
    decimal PayerTariff = 0m
);

public interface IBedOccupancyQuery
{
    IEnumerable<BedOccupantItem> GetActiveOccupants(string? bangsalId, DateTime cutOffDate);
    BedOccupantItem? GetOccupant(string pakaiBedId);
}
