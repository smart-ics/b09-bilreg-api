using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.BedUsageContext.BedOperationalFeature;

public record BedReadinessTransactionModel
{
    private const string ID_PREFIX = "BRT";

    public BedReadinessTransactionModel(
        string transactionId,
        string bedId,
        BedReadinessStatusEnum newStatus,
        BedRestrictionTypeEnum restrictionType,
        string reason,
        string evidenceReference,
        string responsibleActorId,
        string verifiedByActorId,
        DateTime occurredAt,
        DateTime recordedAt,
        string sourceFactId,
        string requestId)
    {
        TransactionId = transactionId;
        BedId = bedId;
        NewStatus = newStatus;
        RestrictionType = restrictionType;
        Reason = reason;
        EvidenceReference = evidenceReference;
        ResponsibleActorId = responsibleActorId;
        VerifiedByActorId = verifiedByActorId;
        OccurredAt = occurredAt;
        RecordedAt = recordedAt;
        SourceFactId = sourceFactId;
        RequestId = requestId;
    }

    #region CREATION

    internal static BedReadinessTransactionModel Record(
        string bedId,
        BedReadinessStatusEnum newStatus,
        BedRestrictionTypeEnum restrictionType,
        string reason,
        string evidenceReference,
        string responsibleActorId,
        DateTime occurredAt,
        DateTime recordedAt,
        string sourceFactId,
        string requestId)
    {
        if (newStatus == BedReadinessStatusEnum.Ready)
            throw new InvalidOperationException(
                "Status Ready harus dicatat melalui verifikasi readiness.");

        ValidateCommon(
            bedId,
            newStatus,
            restrictionType,
            responsibleActorId,
            occurredAt,
            recordedAt,
            requestId);
        Guard.Against.NullOrWhiteSpace(reason);

        return new BedReadinessTransactionModel(
            NunaId.New(ID_PREFIX),
            bedId,
            newStatus,
            restrictionType,
            reason,
            evidenceReference ?? string.Empty,
            responsibleActorId,
            string.Empty,
            occurredAt,
            recordedAt,
            sourceFactId ?? string.Empty,
            requestId);
    }

    internal static BedReadinessTransactionModel VerifyReady(
        string bedId,
        string reason,
        string evidenceReference,
        string responsibleActorId,
        string verifiedByActorId,
        DateTime occurredAt,
        DateTime recordedAt,
        string sourceFactId,
        string requestId)
    {
        ValidateCommon(
            bedId,
            BedReadinessStatusEnum.Ready,
            BedRestrictionTypeEnum.None,
            responsibleActorId,
            occurredAt,
            recordedAt,
            requestId);
        Guard.Against.NullOrWhiteSpace(verifiedByActorId);

        return new BedReadinessTransactionModel(
            NunaId.New(ID_PREFIX),
            bedId,
            BedReadinessStatusEnum.Ready,
            BedRestrictionTypeEnum.None,
            reason ?? string.Empty,
            evidenceReference ?? string.Empty,
            responsibleActorId,
            verifiedByActorId,
            occurredAt,
            recordedAt,
            sourceFactId ?? string.Empty,
            requestId);
    }

    public static BedReadinessTransactionModel Default => new(
        "-",
        "-",
        BedReadinessStatusEnum.CleaningRequired,
        BedRestrictionTypeEnum.Cleaning,
        "-",
        string.Empty,
        "-",
        string.Empty,
        BedOperationalModel.EmptyDate,
        BedOperationalModel.EmptyDate,
        string.Empty,
        "-");

    #endregion

    #region PROPERTIES

    public string TransactionId { get; init; }
    public string BedId { get; init; }
    public BedReadinessStatusEnum NewStatus { get; init; }
    public BedRestrictionTypeEnum RestrictionType { get; init; }
    public string Reason { get; init; }
    public string EvidenceReference { get; init; }
    public string ResponsibleActorId { get; init; }
    public string VerifiedByActorId { get; init; }
    public DateTime OccurredAt { get; init; }
    public DateTime RecordedAt { get; init; }
    public string SourceFactId { get; init; }
    public string RequestId { get; init; }

    #endregion

    #region HELPERS

    private static void ValidateCommon(
        string bedId,
        BedReadinessStatusEnum newStatus,
        BedRestrictionTypeEnum restrictionType,
        string responsibleActorId,
        DateTime occurredAt,
        DateTime recordedAt,
        string requestId)
    {
        Guard.Against.NullOrWhiteSpace(bedId);
        Guard.Against.NullOrWhiteSpace(responsibleActorId);
        Guard.Against.NullOrWhiteSpace(requestId);

        if (!Enum.IsDefined(newStatus))
            throw new ArgumentOutOfRangeException(nameof(newStatus), newStatus, null);
        if (!Enum.IsDefined(restrictionType))
            throw new ArgumentOutOfRangeException(nameof(restrictionType), restrictionType, null);
        if (newStatus == BedReadinessStatusEnum.Ready &&
            restrictionType != BedRestrictionTypeEnum.None)
            throw new InvalidOperationException("Status Ready tidak boleh memiliki restriction.");
        if (newStatus != BedReadinessStatusEnum.Ready &&
            restrictionType == BedRestrictionTypeEnum.None)
            throw new InvalidOperationException("Status non-Ready harus memiliki restriction.");

        BedOperationalModel.EnsureUtc(occurredAt, nameof(occurredAt));
        BedOperationalModel.EnsureUtc(recordedAt, nameof(recordedAt));
        if (recordedAt < occurredAt)
            throw new ArgumentException(
                $"RecordedAt ({recordedAt:O}) tidak boleh lebih awal dari OccurredAt ({occurredAt:O}).",
                nameof(recordedAt));
    }

    #endregion
}
