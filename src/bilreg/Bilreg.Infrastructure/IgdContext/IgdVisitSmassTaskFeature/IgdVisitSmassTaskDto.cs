using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;

namespace Bilreg.Infrastructure.IgdContext.IgdVisitSmassTaskFeature;

public record IgdVisitSmassTaskDto(
    string IgdVisitSmassTaskId,
    string IgdVisitId,
    int NoTriage,
    int TaskType,
    int TaskStatus,
    string AssessmentId,
    int RetryCount,
    DateTime LastRetryDate,
    DateTime ProcessedDate,
    string LastError,
    DateTime CrtDate)
{
    public static IgdVisitSmassTaskDto FromModel(IgdVisitSmassTaskModel model)
        => new(
            model.IgdVisitSmassTaskId,
            model.IgdVisitId,
            model.NoTriage,
            (int)model.TaskType,
            (int)model.TaskStatus,
            model.AssessmentId,
            model.RetryCount,
            model.LastRetryDate,
            model.ProcessedDate,
            model.LastError,
            model.CrtDate);

    public IgdVisitSmassTaskModel ToModel()
        => IgdVisitSmassTaskModel.Rehydrate(
            IgdVisitSmassTaskId,
            IgdVisitId,
            NoTriage,
            (SmassTaskTypeEnum)TaskType,
            (SmassTaskStatusEnum)TaskStatus,
            AssessmentId,
            RetryCount,
            LastRetryDate,
            ProcessedDate,
            LastError,
            CrtDate);
}
