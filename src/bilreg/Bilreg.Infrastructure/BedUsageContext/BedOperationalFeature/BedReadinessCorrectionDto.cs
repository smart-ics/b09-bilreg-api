using Bilreg.Domain.BedUsageContext.BedOperationalFeature;

namespace Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;

public record BedReadinessCorrectionDto(
    string CorrectionId,
    string BedId,
    string OriginalTransactionId,
    string ReplacementTransactionId,
    string ActorId,
    string Reason,
    DateTime OccurredAt,
    DateTime RecordedAt,
    string RequestId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static BedReadinessCorrectionDto FromModel(
        BedReadinessCorrectionModel model) =>
        new(
            model.CorrectionId,
            model.BedId,
            model.OriginalTransactionId,
            model.ReplacementTransactionId,
            model.ActorId,
            model.Reason,
            model.OccurredAt,
            model.RecordedAt,
            model.RequestId,
            model.ActorId,
            model.RecordedAt,
            string.Empty,
            EmptyDate,
            string.Empty,
            EmptyDate);

    public BedReadinessCorrectionModel ToModel() =>
        new(
            CorrectionId,
            BedId,
            OriginalTransactionId,
            ReplacementTransactionId,
            ActorId,
            Reason,
            AsUtc(OccurredAt),
            AsUtc(RecordedAt),
            RequestId);

    private static DateTime AsUtc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
