using Bilreg.Application.BedUsageContext.PakaiBedFeature;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public record PakaiBedActiveOccupancyDto
{
    public string PakaiBedId { get; init; } = string.Empty;
    public string RegId { get; init; } = string.Empty;
    public string PasienId { get; init; } = string.Empty;
    public string PasienName { get; init; } = string.Empty;
    public string BangsalId { get; init; } = string.Empty;
    public string BangsalName { get; init; } = string.Empty;
    public string KamarId { get; init; } = string.Empty;
    public string KamarName { get; init; } = string.Empty;
    public string BedId { get; init; } = string.Empty;
    public string BedName { get; init; } = string.Empty;
    public DateTime TglJamMasuk { get; init; }
    public string BillingKelasId { get; init; } = string.Empty;
    public string BillingKelasName { get; init; } = string.Empty;
    public bool IsRoomingIn { get; init; }
    public string MotherPakaiBedId { get; init; } = string.Empty;

    public PakaiBedActiveOccupancyResponse ToResponse() => new(
        PakaiBedId,
        RegId,
        PasienId,
        PasienName,
        BangsalId,
        BangsalName,
        KamarId,
        KamarName,
        BedId,
        BedName,
        TglJamMasuk,
        BillingKelasId,
        BillingKelasName,
        IsRoomingIn,
        MotherPakaiBedId);
}
