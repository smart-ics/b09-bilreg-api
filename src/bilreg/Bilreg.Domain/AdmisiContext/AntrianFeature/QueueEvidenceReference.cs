using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

/// <summary>
/// Composite Evidence Reference for a Queue Entry when no primary source transaction exists (BR-TRK-013).
/// Serialized as {AntrianId}/No.{NoUrut}.
/// </summary>
public readonly record struct QueueEvidenceReference(string AntrianId, int NoUrut)
{
    public static QueueEvidenceReference Create(string antrianId, int noUrut)
    {
        Guard.Against.NullOrWhiteSpace(antrianId, nameof(antrianId));
        if (noUrut <= 0)
            throw new ArgumentOutOfRangeException(nameof(noUrut), "Queue number must be positive.");

        return new QueueEvidenceReference(antrianId.Trim(), noUrut);
    }

    public string Value => $"{AntrianId}/No.{NoUrut}";

    public override string ToString() => Value;
}
