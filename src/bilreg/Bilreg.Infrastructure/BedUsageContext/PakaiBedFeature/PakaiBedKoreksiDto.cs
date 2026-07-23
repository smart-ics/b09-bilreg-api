using Bilreg.Domain.BedUsageContext.PakaiBedFeature;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public record PakaiBedKoreksiDto(
    string CorrectionId,
    string PakaiBedId,
    string OriginalTransitionId,
    int PakaiBedCorrection,
    string OriginalBangsalId,
    string Reason,
    string CorrectedBy,
    DateTime OccurredAt,
    DateTime RecordedAt,
    string ReviewReference,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static PakaiBedKoreksiDto FromModel(PakaiBedKoreksiModel model) => new(
        model.CorrectionId, model.PakaiBedId, model.OriginalTransitionId,
        (int)model.PakaiBedCorrection, model.OriginalBangsalId, model.Reason,
        model.CorrectedBy, model.OccurredAt, model.RecordedAt, model.ReviewReference,
        model.CorrectedBy, model.RecordedAt, string.Empty, EmptyDate, string.Empty, EmptyDate);

    public PakaiBedKoreksiModel ToModel() => new(
        CorrectionId, PakaiBedId, OriginalTransitionId,
        (PakaiBedKoreksiEnum)PakaiBedCorrection, OriginalBangsalId, Reason,
        CorrectedBy, AsUtc(OccurredAt), AsUtc(RecordedAt), ReviewReference);

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
