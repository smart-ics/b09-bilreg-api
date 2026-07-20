using Ardalis.GuardClauses;

namespace Bilreg.Domain.BedUsageContext.BedOperationalFeature;

public record OccupancyPolicyReff
{
    public OccupancyPolicyReff(string occupancyPolicyId, string displayName)
    {
        Guard.Against.NullOrWhiteSpace(occupancyPolicyId);
        Guard.Against.NullOrWhiteSpace(displayName);

        OccupancyPolicyId = occupancyPolicyId;
        DisplayName = displayName;
    }

    public string OccupancyPolicyId { get; init; }
    public string DisplayName { get; init; }

    public static OccupancyPolicyReff Default => new("-", "-");
}
