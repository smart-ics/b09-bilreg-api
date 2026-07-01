using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public record BilrgMergeRequestDto(
    string MergeRequestId,
    string SourceRegId,
    string TargetRegId,
    string PatientId,
    int Status,
    string Reason,
    string ExecutedBy,
    DateTime ExecutedDate,
    string CancelledBy,
    DateTime CancelledDate,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static BilrgMergeRequestDto FromModelForInsert(MergeRequestModel model, string patientId, string actor) =>
        new(
            model.MergeRequestId,
            model.SourceRegId,
            model.TargetRegId ?? string.Empty,
            patientId,
            (int)model.Status,
            string.Empty,
            string.Empty,
            EmptyDate,
            string.Empty,
            EmptyDate,
            actor,
            DateTime.Now,
            actor,
            DateTime.Now,
            string.Empty,
            EmptyDate);

    public static BilrgMergeRequestDto FromModelForUpdate(
        MergeRequestModel model,
        string patientId,
        string actor,
        MergeRequestStatusEnum previousStatus)
    {
        var now = DateTime.Now;
        var executedBy = previousStatus == MergeRequestStatusEnum.Pending &&
                         model.Status == MergeRequestStatusEnum.Executed
            ? actor
            : string.Empty;
        var executedDate = !string.IsNullOrEmpty(executedBy) ? now : EmptyDate;
        var cancelledBy = previousStatus == MergeRequestStatusEnum.Pending &&
                            model.Status == MergeRequestStatusEnum.Cancelled
            ? actor
            : string.Empty;
        var cancelledDate = !string.IsNullOrEmpty(cancelledBy) ? now : EmptyDate;

        return new BilrgMergeRequestDto(
            model.MergeRequestId,
            model.SourceRegId,
            model.TargetRegId ?? string.Empty,
            patientId,
            (int)model.Status,
            string.Empty,
            executedBy,
            executedDate,
            cancelledBy,
            cancelledDate,
            string.Empty,
            EmptyDate,
            actor,
            now,
            string.Empty,
            EmptyDate);
    }

    public MergeRequestModel ToModel() =>
        RehydrateMergeRequest(
            MergeRequestId,
            SourceRegId,
            string.IsNullOrWhiteSpace(TargetRegId) ? null : TargetRegId,
            (MergeRequestStatusEnum)Status);

    private static MergeRequestModel RehydrateMergeRequest(
        string mergeRequestId,
        string sourceRegId,
        string? targetRegId,
        MergeRequestStatusEnum status)
    {
        var model = MergeRequestModel.Create(mergeRequestId, sourceRegId, targetRegId);
        switch (status)
        {
            case MergeRequestStatusEnum.Executed:
                if (!string.IsNullOrWhiteSpace(targetRegId))
                    model.AssignTarget(targetRegId);
                model.Execute();
                break;
            case MergeRequestStatusEnum.Cancelled:
                model.Cancel();
                break;
        }

        return model;
    }
}
