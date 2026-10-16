namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public record PakaiBedActiveOccupancyResponse(
    string PakaiBedId,
    string RegId,
    string PasienId,
    string PasienName,
    string BangsalId,
    string BangsalName,
    string KamarId,
    string KamarName,
    string BedId,
    string BedName,
    DateTime TglJamMasuk,
    string BillingKelasId,
    string BillingKelasName,
    bool IsRoomingIn,
    string MotherPakaiBedId)
{
    public string NomorBed => BedName;
    public string NamaPasien => PasienName;
    public string BillingClass => BillingKelasId;
    public bool StatusRawatGabung => IsRoomingIn;
}
