using Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

namespace Bilreg.Infrastructure.BedUsageContext.RnaServiceExecutionFeature;

public record ExecutionCorrectionDto(
    string CorrectionFactId, string ServiceExecutionId, string OriginalServiceExecutionFactId,
    int PreviousRevision, int CorrectionRevision, int CorrectionKind,
    string ReplacementServiceExecutionFactId, string CorrectionReason, string CorrectingActorId,
    string SecondReviewerId, string EvidenceReference, DateTime CorrectedAt, DateTime RecordedAt,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate, string VodUser, DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);
    public static ExecutionCorrectionDto FromModel(string serviceExecutionId, ExecutionCorrectionModel model) => new(
        model.CorrectionFactId, serviceExecutionId, model.OriginalServiceExecutionFactId,
        model.PreviousRevision, model.CorrectionRevision, (int)model.CorrectionKind,
        model.ReplacementServiceExecutionFactId, model.CorrectionReason, model.CorrectingActorId,
        model.SecondReviewerId, model.EvidenceReference, model.CorrectedAt, model.RecordedAt,
        model.CorrectingActorId, model.RecordedAt, string.Empty, EmptyDate, string.Empty, EmptyDate);
    public ExecutionCorrectionModel ToModel() => new(CorrectionFactId, OriginalServiceExecutionFactId,
        PreviousRevision, CorrectionRevision, (ExecutionCorrectionKindEnum)CorrectionKind,
        ReplacementServiceExecutionFactId, CorrectionReason, CorrectingActorId, SecondReviewerId,
        EvidenceReference, AsUtc(CorrectedAt), AsUtc(RecordedAt));
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
