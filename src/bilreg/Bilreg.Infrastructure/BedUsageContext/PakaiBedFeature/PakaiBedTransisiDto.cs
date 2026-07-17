using Bilreg.Domain.BedUsageContext.PakaiBedFeature;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public record PakaiBedTransisiDto(
    string TransitionId,
    string PakaiBedId,
    int PakaiBedTransition,
    DateTime OccurredAt,
    DateTime RecordedAt,
    string ActorId,
    string Reason,
    string RequestId,
    string WaitingListId,
    string BedAssignabilityEvidenceId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static PakaiBedTransisiDto FromModel(PakaiBedTransisiModel model) => new(
        model.TransitionId, model.PakaiBedId, (int)model.PakaiBedTransition,
        model.OccurredAt, model.RecordedAt, model.ActorId, model.Reason,
        model.RequestId, model.WaitingListId, model.BedAssignabilityEvidenceId,
        model.ActorId, model.RecordedAt, string.Empty, EmptyDate, string.Empty, EmptyDate);

    public PakaiBedTransisiModel ToModel() => new(
        TransitionId, PakaiBedId, (PakaiBedTransisiEnum)PakaiBedTransition,
        AsUtc(OccurredAt), AsUtc(RecordedAt), ActorId, Reason, RequestId,
        WaitingListId, BedAssignabilityEvidenceId);

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
