using Ardalis.GuardClauses;

namespace Farinv.Domain.SalesContext.AntrianFeature;

/// <summary>
/// Cross-context queue evidence reference for pharmacy Tracker events (BR-TRK-013).
/// </summary>
public static class PharmacyQueueEvidenceReference
{
    public static string Create(string antrianId, int noAntrian)
    {
        Guard.Against.NullOrWhiteSpace(antrianId, nameof(antrianId));
        if (noAntrian <= 0)
            throw new ArgumentOutOfRangeException(nameof(noAntrian), "Queue number must be positive.");

        return $"{antrianId.Trim()}/No.{noAntrian}";
    }
}
