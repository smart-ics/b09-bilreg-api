using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

public record ExecutionCorrectionModel(
    string CorrectionFactId,
    string OriginalServiceExecutionFactId,
    int PreviousRevision,
    int CorrectionRevision,
    ExecutionCorrectionKindEnum CorrectionKind,
    string ReplacementServiceExecutionFactId,
    string CorrectionReason,
    string CorrectingActorId,
    string SecondReviewerId,
    string EvidenceReference,
    DateTime CorrectedAt,
    DateTime RecordedAt)
{
    private const string ID_PREFIX = "SEC";

    internal static ExecutionCorrectionModel Create(
        string originalServiceExecutionFactId,
        int previousRevision,
        int correctionRevision,
        ExecutionCorrectionKindEnum correctionKind,
        string? replacementServiceExecutionFactId,
        string correctionReason,
        string correctingActorId,
        string? secondReviewerId,
        string? evidenceReference,
        DateTime correctedAt,
        DateTime recordedAt = default)
    {
        Guard.Against.NullOrWhiteSpace(originalServiceExecutionFactId);
        Guard.Against.NullOrWhiteSpace(correctionReason);
        Guard.Against.NullOrWhiteSpace(correctingActorId);
        if (previousRevision <= 0 || correctionRevision <= previousRevision)
            throw new ArgumentOutOfRangeException(nameof(correctionRevision));
        if (!Enum.IsDefined(correctionKind))
            throw new ArgumentOutOfRangeException(nameof(correctionKind));
        EnsureUtc(correctedAt, nameof(correctedAt));
        EnsureUtc(recordedAt, nameof(recordedAt));
        if (recordedAt < correctedAt)
            throw new ArgumentException("RecordedAt tidak boleh lebih awal dari CorrectedAt.", nameof(recordedAt));
        if (correctionKind == ExecutionCorrectionKindEnum.EnteredInError &&
            !string.IsNullOrWhiteSpace(replacementServiceExecutionFactId))
            throw new ArgumentException("Entered in Error tidak boleh memiliki replacement fact.", nameof(replacementServiceExecutionFactId));
        if (correctionKind != ExecutionCorrectionKindEnum.EnteredInError &&
            string.IsNullOrWhiteSpace(replacementServiceExecutionFactId))
            throw new ArgumentException("Correction atau replacement wajib memiliki replacement fact.", nameof(replacementServiceExecutionFactId));

        return new ExecutionCorrectionModel(
            NunaId.New(ID_PREFIX),
            originalServiceExecutionFactId,
            previousRevision,
            correctionRevision,
            correctionKind,
            replacementServiceExecutionFactId ?? string.Empty,
            correctionReason,
            correctingActorId,
            secondReviewerId ?? string.Empty,
            evidenceReference ?? string.Empty,
            correctedAt,
            recordedAt);
    }

    public static ExecutionCorrectionModel Default => new(
        "-", "-", 0, 0, ExecutionCorrectionKindEnum.Corrected, string.Empty,
        "-", "-", string.Empty, string.Empty,
        RnaServiceExecutionModel.EmptyDate, RnaServiceExecutionModel.EmptyDate);

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"{parameterName} harus menggunakan DateTimeKind.Utc.", parameterName);
    }
}
