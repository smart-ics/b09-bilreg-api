using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.BedUsageContext.PakaiBedFeature;

public record PakaiBedTransisiModel
{
    private const string ID_PREFIX = "ATR";

    public PakaiBedTransisiModel(
        string transitionId,
        string pakaiBedId,
        PakaiBedTransisiEnum pakaiBedTransition,
        DateTime occurredAt,
        DateTime recordedAt,
        string actorId,
        string reason,
        string requestId,
        string waitingListId,
        string bedAssignabilityEvidenceId)
    {
        TransitionId = transitionId;
        PakaiBedId = pakaiBedId;
        PakaiBedTransition = pakaiBedTransition;
        OccurredAt = occurredAt;
        RecordedAt = recordedAt;
        ActorId = actorId;
        Reason = reason;
        RequestId = requestId;
        WaitingListId = waitingListId;
        BedAssignabilityEvidenceId = bedAssignabilityEvidenceId;
    }

    #region CREATION

    internal static PakaiBedTransisiModel Propose(
        string pakaiBedId,
        DateTime occurredAt,
        DateTime recordedAt,
        string actorId,
        string reason,
        string requestId,
        string waitingListId)
    {
        ValidateCommon(pakaiBedId, occurredAt, recordedAt, actorId, reason, requestId);

        return new PakaiBedTransisiModel(
            NunaId.New(ID_PREFIX),
            pakaiBedId,
            PakaiBedTransisiEnum.Proposed,
            occurredAt,
            recordedAt,
            actorId,
            reason,
            requestId,
            waitingListId,
            string.Empty);
    }

    internal static PakaiBedTransisiModel Assign(
        string pakaiBedId,
        DateTime occurredAt,
        DateTime recordedAt,
        string actorId,
        string reason,
        string requestId,
        string waitingListId,
        string bedAssignabilityEvidenceId)
    {
        ValidateCommon(pakaiBedId, occurredAt, recordedAt, actorId, reason, requestId);
        Guard.Against.NullOrWhiteSpace(bedAssignabilityEvidenceId);

        return new PakaiBedTransisiModel(
            NunaId.New(ID_PREFIX),
            pakaiBedId,
            PakaiBedTransisiEnum.Assigned,
            occurredAt,
            recordedAt,
            actorId,
            reason,
            requestId,
            waitingListId,
            bedAssignabilityEvidenceId);
    }

    public static PakaiBedTransisiModel Default => new(
        "-",
        "-",
        PakaiBedTransisiEnum.Proposed,
        PakaiBedAlokasiModel.EmptyDate,
        PakaiBedAlokasiModel.EmptyDate,
        "-",
        "-",
        "-",
        string.Empty,
        string.Empty);

    #endregion

    #region PROPERTIES

    public string TransitionId { get; init; }
    public string PakaiBedId { get; init; }
    public PakaiBedTransisiEnum PakaiBedTransition { get; init; }
    public DateTime OccurredAt { get; init; }
    public DateTime RecordedAt { get; init; }
    public string ActorId { get; init; }
    public string Reason { get; init; }
    public string RequestId { get; init; }
    public string WaitingListId { get; init; }
    public string BedAssignabilityEvidenceId { get; init; }

    #endregion

    #region HELPERS

    private static void ValidateCommon(
        string pakaiBedId,
        DateTime occurredAt,
        DateTime recordedAt,
        string actorId,
        string reason,
        string requestId)
    {
        Guard.Against.NullOrWhiteSpace(pakaiBedId);
        Guard.Against.NullOrWhiteSpace(actorId);
        Guard.Against.NullOrWhiteSpace(reason);
        Guard.Against.NullOrWhiteSpace(requestId);
        PakaiBedAlokasiModel.EnsureUtc(occurredAt, nameof(occurredAt));
        PakaiBedAlokasiModel.EnsureUtc(recordedAt, nameof(recordedAt));

        if (recordedAt < occurredAt)
            throw new ArgumentException(
                $"RecordedAt ({recordedAt:O}) tidak boleh lebih awal dari OccurredAt ({occurredAt:O}).",
                nameof(recordedAt));
    }

    #endregion
}
