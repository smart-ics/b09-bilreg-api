using Bilreg.Domain.BedUsageContext.BedOperationalFeature;

namespace Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;

public record BedReadinessTransactionDto(
    string TransactionId,
    string BedId,
    int NewStatus,
    int RestrictionType,
    string Reason,
    string EvidenceReference,
    string ResponsibleActorId,
    string VerifiedByActorId,
    DateTime OccurredAt,
    DateTime RecordedAt,
    string SourceFactId,
    string RequestId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static BedReadinessTransactionDto FromModel(
        BedReadinessTransactionModel model) =>
        new(
            model.TransactionId,
            model.BedId,
            (int)model.NewStatus,
            (int)model.RestrictionType,
            model.Reason,
            model.EvidenceReference,
            model.ResponsibleActorId,
            model.VerifiedByActorId,
            model.OccurredAt,
            model.RecordedAt,
            model.SourceFactId,
            model.RequestId,
            model.ResponsibleActorId,
            model.RecordedAt,
            string.Empty,
            EmptyDate,
            string.Empty,
            EmptyDate);

    public BedReadinessTransactionModel ToModel() =>
        new(
            TransactionId,
            BedId,
            (BedReadinessStatusEnum)NewStatus,
            (BedRestrictionTypeEnum)RestrictionType,
            Reason,
            EvidenceReference,
            ResponsibleActorId,
            VerifiedByActorId,
            AsUtc(OccurredAt),
            AsUtc(RecordedAt),
            SourceFactId,
            RequestId);

    private static DateTime AsUtc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
