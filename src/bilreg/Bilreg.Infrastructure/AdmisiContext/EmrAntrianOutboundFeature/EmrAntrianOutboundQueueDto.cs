using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;

namespace Bilreg.Infrastructure.AdmisiContext.EmrAntrianOutboundFeature;

public record EmrAntrianOutboundQueueDto(
    string QueueId,
    string SourceId,
    string MessageType,
    string PayloadJson,
    int QueueStatus,
    int RetryCount,
    DateTime LastRetryDate,
    DateTime ProcessedDate,
    string LastError,
    DateTime CrtDate)
{
    public static EmrAntrianOutboundQueueDto FromModel(EmrAntrianOutboundQueueModel model)
        => new(
            model.QueueId,
            model.SourceId,
            model.MessageType,
            model.PayloadJson,
            (int)model.QueueStatus,
            model.RetryCount,
            model.LastRetryDate,
            model.ProcessedDate,
            model.LastError,
            model.CrtDate);

    public EmrAntrianOutboundQueueModel ToModel()
        => EmrAntrianOutboundQueueModel.Rehydrate(
            QueueId,
            SourceId,
            MessageType,
            PayloadJson,
            (EmrAntrianOutboundQueueStatusEnum)QueueStatus,
            RetryCount,
            LastRetryDate,
            ProcessedDate,
            LastError,
            CrtDate);
}
