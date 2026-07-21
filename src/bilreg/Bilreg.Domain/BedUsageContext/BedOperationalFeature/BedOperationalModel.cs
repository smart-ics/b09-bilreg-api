using Ardalis.GuardClauses;

namespace Bilreg.Domain.BedUsageContext.BedOperationalFeature;

public record BedOperationalModel : IBedOperationalKey
{
    internal static readonly DateTime EmptyDate =
        new(3000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly List<BedReadinessTransactionModel> _listReadinessTransaction;
    private readonly List<BedReadinessCorrectionModel> _listReadinessCorrection;

    public BedOperationalModel(
        string bedId,
        string bangsalId,
        string kamarId,
        OccupancyPolicyReff occupancyPolicy,
        BedReadinessStatusEnum? currentReadiness,
        string latestReadinessTransactionId,
        BedBlockerStateType currentBlockerState,
        long occupancyEpoch,
        int version,
        IEnumerable<BedReadinessTransactionModel> listReadinessTransaction,
        IEnumerable<BedReadinessCorrectionModel> listReadinessCorrection)
    {
        BedId = bedId;
        BangsalId = bangsalId;
        KamarId = kamarId;
        OccupancyPolicy = occupancyPolicy;
        CurrentReadiness = currentReadiness;
        LatestReadinessTransactionId = latestReadinessTransactionId;
        CurrentBlockerState = currentBlockerState;
        OccupancyEpoch = occupancyEpoch;
        Version = version;
        _listReadinessTransaction = listReadinessTransaction?.ToList() ?? [];
        _listReadinessCorrection = listReadinessCorrection?.ToList() ?? [];
    }

    #region CREATION

    public static BedOperationalModel Create(
        string bedId,
        string bangsalId,
        string kamarId,
        OccupancyPolicyReff occupancyPolicy)
    {
        Guard.Against.NullOrWhiteSpace(bedId);
        Guard.Against.NullOrWhiteSpace(bangsalId);
        Guard.Against.NullOrWhiteSpace(kamarId);
        Guard.Against.Null(occupancyPolicy);

        return new BedOperationalModel(
            bedId,
            bangsalId,
            kamarId,
            occupancyPolicy,
            null,
            string.Empty,
            BedBlockerStateType.None,
            0,
            0,
            [],
            []);
    }

    public static BedOperationalModel Default => new(
        "-",
        "-",
        "-",
        OccupancyPolicyReff.Default,
        null,
        string.Empty,
        BedBlockerStateType.None,
        0,
        0,
        [],
        []);

    public static IBedOperationalKey Key(string bedId)
    {
        Guard.Against.NullOrWhiteSpace(bedId);
        return Default with { BedId = bedId };
    }

    #endregion

    #region PROPERTIES

    public string BedId { get; init; }
    public string BangsalId { get; init; }
    public string KamarId { get; init; }
    public OccupancyPolicyReff OccupancyPolicy { get; init; }
    public BedReadinessStatusEnum? CurrentReadiness { get; init; }
    public string LatestReadinessTransactionId { get; init; }
    public BedBlockerStateType CurrentBlockerState { get; init; }
    public long OccupancyEpoch { get; init; }
    public int Version { get; init; }
    public IEnumerable<BedReadinessTransactionModel> ListReadinessTransaction =>
        _listReadinessTransaction.AsReadOnly();
    public IEnumerable<BedReadinessCorrectionModel> ListReadinessCorrection =>
        _listReadinessCorrection.AsReadOnly();

    public bool IsReadinessVerified =>
        CurrentReadiness == BedReadinessStatusEnum.Ready &&
        !string.IsNullOrWhiteSpace(LatestReadinessTransactionId);

    public bool HasUnresolvedBlocker => CurrentBlockerState.IsBlocked;

    #endregion

    #region BEHAVIOUR

    public BedOperationalModel RecordReadiness(
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
        EnsureSourceFactIsUnique(sourceFactId);
        var normalizedRestriction = NormalizeRestriction(newStatus, restrictionType);
        var transaction = BedReadinessTransactionModel.Record(
            BedId,
            newStatus,
            normalizedRestriction,
            reason,
            evidenceReference,
            responsibleActorId,
            occurredAt,
            recordedAt,
            sourceFactId,
            requestId);

        return WithHistory(
            _listReadinessTransaction.Append(transaction),
            _listReadinessCorrection,
            Version + 1,
            OccupancyEpoch);
    }

    public BedOperationalModel VerifyReady(
        string reason,
        string evidenceReference,
        string responsibleActorId,
        string verifiedByActorId,
        DateTime occurredAt,
        DateTime recordedAt,
        string sourceFactId,
        string requestId)
    {
        EnsureSourceFactIsUnique(sourceFactId);
        var transaction = BedReadinessTransactionModel.VerifyReady(
            BedId,
            reason,
            evidenceReference,
            responsibleActorId,
            verifiedByActorId,
            occurredAt,
            recordedAt,
            sourceFactId,
            requestId);

        return WithHistory(
            _listReadinessTransaction.Append(transaction),
            _listReadinessCorrection,
            Version + 1,
            OccupancyEpoch);
    }

    public BedOperationalModel CorrectReadiness(
        string originalTransactionId,
        BedReadinessStatusEnum replacementStatus,
        BedRestrictionTypeEnum replacementRestrictionType,
        string replacementReason,
        string correctionReason,
        string replacementEvidenceReference,
        string actorId,
        string verifiedByActorId,
        DateTime correctionOccurredAt,
        DateTime recordedAt,
        string requestId)
    {
        Guard.Against.NullOrWhiteSpace(originalTransactionId);

        var original = _listReadinessTransaction
            .SingleOrDefault(x => x.TransactionId == originalTransactionId)
            ?? throw new InvalidOperationException(
                $"Transaksi readiness {originalTransactionId} tidak ditemukan pada Bed {BedId}.");

        if (_listReadinessCorrection.Any(
                x => x.OriginalTransactionId == originalTransactionId))
            throw new InvalidOperationException(
                $"Transaksi readiness {originalTransactionId} sudah dikoreksi.");

        EnsureUtc(correctionOccurredAt, nameof(correctionOccurredAt));
        if (correctionOccurredAt < original.OccurredAt)
            throw new ArgumentException(
                "Waktu koreksi tidak boleh lebih awal dari transaksi readiness yang dikoreksi.",
                nameof(correctionOccurredAt));

        var replacement = replacementStatus == BedReadinessStatusEnum.Ready
            ? BedReadinessTransactionModel.VerifyReady(
                BedId,
                replacementReason,
                replacementEvidenceReference,
                actorId,
                verifiedByActorId,
                original.OccurredAt,
                recordedAt,
                original.SourceFactId,
                requestId)
            : BedReadinessTransactionModel.Record(
                BedId,
                replacementStatus,
                NormalizeRestriction(replacementStatus, replacementRestrictionType),
                replacementReason,
                replacementEvidenceReference,
                actorId,
                original.OccurredAt,
                recordedAt,
                original.SourceFactId,
                requestId);

        var correction = BedReadinessCorrectionModel.Create(
            BedId,
            originalTransactionId,
            replacement.TransactionId,
            actorId,
            correctionReason,
            correctionOccurredAt,
            recordedAt,
            requestId);

        return WithHistory(
            _listReadinessTransaction.Append(replacement),
            _listReadinessCorrection.Append(correction),
            Version + 1,
            OccupancyEpoch);
    }

    public BedOperationalModel AdvanceOccupancyEpoch()
        => WithHistory(
            _listReadinessTransaction,
            _listReadinessCorrection,
            Version + 1,
            OccupancyEpoch + 1);

    public void AssertReadyForAllocation(BedMasterSnapshotType master)
    {
        Guard.Against.Null(master);

        if (master.BedId != BedId)
            throw new InvalidOperationException(
                $"Bed master {master.BedId} tidak sesuai dengan Bed operational {BedId}.");
        if (master.BangsalId != BangsalId || master.KamarId != KamarId)
            throw new InvalidOperationException(
                $"Lokasi master Bed {BedId} tidak sesuai dengan snapshot operational.");
        if (!master.IsActive)
            throw new InvalidOperationException($"Bed {BedId} tidak aktif pada master.");
        if (!IsReadinessVerified)
            throw new InvalidOperationException(
                $"Readiness Bed {BedId} belum terverifikasi Ready.");
        if (HasUnresolvedBlocker)
            throw new InvalidOperationException(
                $"Bed {BedId} masih memiliki blocker yang belum terselesaikan.");
    }

    #endregion

    #region HELPERS

    internal static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                $"{parameterName} harus menggunakan DateTimeKind.Utc.",
                parameterName);
    }

    private static BedRestrictionTypeEnum NormalizeRestriction(
        BedReadinessStatusEnum status,
        BedRestrictionTypeEnum restrictionType)
    {
        if (status is BedReadinessStatusEnum.CleaningRequired or
            BedReadinessStatusEnum.CleaningInProgress &&
            restrictionType == BedRestrictionTypeEnum.None)
            return BedRestrictionTypeEnum.Cleaning;

        return restrictionType;
    }

    private void EnsureSourceFactIsUnique(string sourceFactId)
    {
        if (!string.IsNullOrWhiteSpace(sourceFactId) &&
            _listReadinessTransaction.Any(x => x.SourceFactId == sourceFactId))
            throw new InvalidOperationException(
                $"Source fact {sourceFactId} sudah dicatat untuk Bed {BedId}.");
    }

    private BedOperationalModel WithHistory(
        IEnumerable<BedReadinessTransactionModel> transactions,
        IEnumerable<BedReadinessCorrectionModel> corrections,
        int version,
        long occupancyEpoch)
    {
        var transactionList = transactions.ToList();
        var correctionList = corrections.ToList();
        var correctedIds = correctionList
            .Select(x => x.OriginalTransactionId)
            .ToHashSet(StringComparer.Ordinal);
        var latest = transactionList
            .Where(x => !correctedIds.Contains(x.TransactionId))
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.TransactionId, StringComparer.Ordinal)
            .LastOrDefault();

        return new BedOperationalModel(
            BedId,
            BangsalId,
            KamarId,
            OccupancyPolicy,
            latest?.NewStatus,
            latest?.TransactionId ?? string.Empty,
            latest is null
                ? BedBlockerStateType.None
                : BedBlockerStateType.From(latest),
            occupancyEpoch,
            version,
            transactionList,
            correctionList);
    }

    #endregion
}

public interface IBedOperationalKey
{
    string BedId { get; }
}
