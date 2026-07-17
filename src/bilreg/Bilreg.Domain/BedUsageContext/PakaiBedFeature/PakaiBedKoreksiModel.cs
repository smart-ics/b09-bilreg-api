using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.BedUsageContext.PakaiBedFeature;

public record PakaiBedKoreksiModel
{
    private const string ID_PREFIX = "ACR";

    public PakaiBedKoreksiModel(
        string correctionId,
        string pakaiBedId,
        string originalTransitionId,
        PakaiBedKoreksiEnum pakaiBedCorrection,
        string originalBangsalId,
        string reason,
        string correctedBy,
        DateTime occurredAt,
        DateTime recordedAt,
        string reviewReference)
    {
        CorrectionId = correctionId;
        PakaiBedId = pakaiBedId;
        OriginalTransitionId = originalTransitionId;
        PakaiBedCorrection = pakaiBedCorrection;
        OriginalBangsalId = originalBangsalId;
        Reason = reason;
        CorrectedBy = correctedBy;
        OccurredAt = occurredAt;
        RecordedAt = recordedAt;
        ReviewReference = reviewReference;
    }

    #region CREATION

    internal static PakaiBedKoreksiModel Create(
        string pakaiBedId,
        string originalTransitionId,
        PakaiBedKoreksiEnum pakaiBedCorrection,
        string originalBangsalId,
        string reason,
        string correctedBy,
        DateTime occurredAt,
        DateTime recordedAt,
        string reviewReference)
    {
        Guard.Against.NullOrWhiteSpace(pakaiBedId);
        Guard.Against.NullOrWhiteSpace(originalTransitionId);
        Guard.Against.NullOrWhiteSpace(originalBangsalId);
        Guard.Against.NullOrWhiteSpace(reason);
        Guard.Against.NullOrWhiteSpace(correctedBy);
        PakaiBedAlokasiModel.EnsureUtc(occurredAt, nameof(occurredAt));
        PakaiBedAlokasiModel.EnsureUtc(recordedAt, nameof(recordedAt));

        if (recordedAt < occurredAt)
            throw new ArgumentException(
                $"RecordedAt ({recordedAt:O}) tidak boleh lebih awal dari OccurredAt ({occurredAt:O}).",
                nameof(recordedAt));

        return new PakaiBedKoreksiModel(
            NunaId.New(ID_PREFIX),
            pakaiBedId,
            originalTransitionId,
            pakaiBedCorrection,
            originalBangsalId,
            reason,
            correctedBy,
            occurredAt,
            recordedAt,
            reviewReference ?? string.Empty);
    }

    public static PakaiBedKoreksiModel Default => new(
        "-",
        "-",
        "-",
        PakaiBedKoreksiEnum.Corrected,
        "-",
        "-",
        "-",
        PakaiBedAlokasiModel.EmptyDate,
        PakaiBedAlokasiModel.EmptyDate,
        string.Empty);

    #endregion

    #region PROPERTIES

    public string CorrectionId { get; init; }
    public string PakaiBedId { get; init; }
    public string OriginalTransitionId { get; init; }
    public PakaiBedKoreksiEnum PakaiBedCorrection { get; init; }
    public string OriginalBangsalId { get; init; }
    public string Reason { get; init; }
    public string CorrectedBy { get; init; }
    public DateTime OccurredAt { get; init; }
    public DateTime RecordedAt { get; init; }
    public string ReviewReference { get; init; }

    #endregion
}
