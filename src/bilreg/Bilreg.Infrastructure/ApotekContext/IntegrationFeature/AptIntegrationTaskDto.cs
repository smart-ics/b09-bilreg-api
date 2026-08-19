using Bilreg.Domain.ApotekContext.IntegrationFeature;

namespace Bilreg.Infrastructure.ApotekContext.IntegrationFeature;

public record AptIntegrationTaskDto(
    string IntegrationTaskId,
    int TaskType,
    int SourceKind,
    string SourceId,
    string IdempotencyKey,
    int Destination,
    string PayloadJson,
    int TaskStatus,
    int RetryCount,
    string LastError,
    DateTime LastRetryDate,
    DateTime ProcessedDate,
    string CorrelationId,
    DateTime CrtDate)
{
    public static AptIntegrationTaskDto FromModel(AptIntegrationTaskModel model)
        => new(
            model.IntegrationTaskId,
            (int)model.TaskType,
            (int)model.SourceKind,
            model.SourceId,
            model.IdempotencyKey,
            (int)model.Destination,
            model.PayloadJson,
            (int)model.TaskStatus,
            model.RetryCount,
            model.LastError,
            model.LastRetryDate,
            model.ProcessedDate,
            model.CorrelationId,
            model.CrtDate);

    public AptIntegrationTaskModel ToModel()
        => AptIntegrationTaskModel.Rehydrate(
            IntegrationTaskId,
            (AptIntegrationTaskTypeEnum)TaskType,
            (AptIntegrationSourceKindEnum)SourceKind,
            SourceId,
            IdempotencyKey,
            (AptIntegrationDestinationEnum)Destination,
            PayloadJson,
            (AptIntegrationTaskStatusEnum)TaskStatus,
            RetryCount,
            LastError,
            LastRetryDate,
            ProcessedDate,
            CorrelationId,
            CrtDate);
}
