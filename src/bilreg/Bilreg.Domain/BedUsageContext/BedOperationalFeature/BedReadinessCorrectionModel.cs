using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.BedUsageContext.BedOperationalFeature;

public record BedReadinessCorrectionModel
{
    private const string ID_PREFIX = "BRC";

    public BedReadinessCorrectionModel(
        string correctionId,
        string bedId,
        string originalTransactionId,
        string replacementTransactionId,
        string actorId,
        string reason,
        DateTime occurredAt,
        DateTime recordedAt,
        string requestId)
    {
        CorrectionId = correctionId;
        BedId = bedId;
        OriginalTransactionId = originalTransactionId;
        ReplacementTransactionId = replacementTransactionId;
        ActorId = actorId;
        Reason = reason;
        OccurredAt = occurredAt;
        RecordedAt = recordedAt;
        RequestId = requestId;
    }

    #region CREATION

    internal static BedReadinessCorrectionModel Create(
        string bedId,
        string originalTransactionId,
        string replacementTransactionId,
        string actorId,
        string reason,
        DateTime occurredAt,
        DateTime recordedAt,
        string requestId)
    {
        Guard.Against.NullOrWhiteSpace(bedId);
        Guard.Against.NullOrWhiteSpace(originalTransactionId);
        Guard.Against.NullOrWhiteSpace(replacementTransactionId);
        Guard.Against.NullOrWhiteSpace(actorId);
        Guard.Against.NullOrWhiteSpace(reason);
        Guard.Against.NullOrWhiteSpace(requestId);
        BedOperationalModel.EnsureUtc(occurredAt, nameof(occurredAt));
        BedOperationalModel.EnsureUtc(recordedAt, nameof(recordedAt));

        if (recordedAt < occurredAt)
            throw new ArgumentException(
                $"RecordedAt ({recordedAt:O}) tidak boleh lebih awal dari OccurredAt ({occurredAt:O}).",
                nameof(recordedAt));

        return new BedReadinessCorrectionModel(
            NunaId.New(ID_PREFIX),
            bedId,
            originalTransactionId,
            replacementTransactionId,
            actorId,
            reason,
            occurredAt,
            recordedAt,
            requestId);
    }

    public static BedReadinessCorrectionModel Default => new(
        "-",
        "-",
        "-",
        "-",
        "-",
        "-",
        BedOperationalModel.EmptyDate,
        BedOperationalModel.EmptyDate,
        "-");

    #endregion

    #region PROPERTIES

    public string CorrectionId { get; init; }
    public string BedId { get; init; }
    public string OriginalTransactionId { get; init; }
    public string ReplacementTransactionId { get; init; }
    public string ActorId { get; init; }
    public string Reason { get; init; }
    public DateTime OccurredAt { get; init; }
    public DateTime RecordedAt { get; init; }
    public string RequestId { get; init; }

    #endregion
}
