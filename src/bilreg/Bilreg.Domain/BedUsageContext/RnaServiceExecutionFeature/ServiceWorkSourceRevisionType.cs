using Ardalis.GuardClauses;

namespace Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

public record ServiceWorkSourceRevisionType(
    string SourceContext,
    string SourceFactId,
    int SourceRevision,
    SourceRevisionKindEnum RevisionKind,
    DateTime EffectiveAt,
    DateTime RecordedAt,
    string ActorId,
    string Reason)
{
    internal static ServiceWorkSourceRevisionType Create(
        string sourceContext,
        string sourceFactId,
        int sourceRevision,
        SourceRevisionKindEnum revisionKind,
        DateTime effectiveAt,
        DateTime recordedAt,
        string actorId,
        string? reason)
    {
        Guard.Against.NullOrWhiteSpace(sourceContext);
        Guard.Against.NullOrWhiteSpace(sourceFactId);
        Guard.Against.NullOrWhiteSpace(actorId);
        if (sourceRevision <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceRevision));
        if (!Enum.IsDefined(revisionKind))
            throw new ArgumentOutOfRangeException(nameof(revisionKind));
        if (effectiveAt.Kind != DateTimeKind.Utc || recordedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("EffectiveAt dan RecordedAt harus menggunakan DateTimeKind.Utc.");
        return new ServiceWorkSourceRevisionType(
            sourceContext, sourceFactId, sourceRevision, revisionKind, effectiveAt, recordedAt, actorId, reason ?? string.Empty);
    }
}
