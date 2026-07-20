using Ardalis.GuardClauses;

namespace Bilreg.Domain.BedUsageContext.BedOperationalFeature;

public record BedMasterSnapshotType
{
    public BedMasterSnapshotType(
        string bedId,
        string bangsalId,
        string kamarId,
        bool isActive)
    {
        Guard.Against.NullOrWhiteSpace(bedId);
        Guard.Against.NullOrWhiteSpace(bangsalId);
        Guard.Against.NullOrWhiteSpace(kamarId);

        BedId = bedId;
        BangsalId = bangsalId;
        KamarId = kamarId;
        IsActive = isActive;
    }

    public string BedId { get; init; }
    public string BangsalId { get; init; }
    public string KamarId { get; init; }
    public bool IsActive { get; init; }

    public static BedMasterSnapshotType Default => new("-", "-", "-", false);
}
