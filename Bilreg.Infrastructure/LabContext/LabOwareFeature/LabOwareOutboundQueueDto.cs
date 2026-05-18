using Bilreg.Domain.LabContext.LabOwareFeature;

namespace Bilreg.Infrastructure.LabContext.LabOwareFeature;

public record LabOwareOutboundQueueDto(
    string QueueId,
    string OrderId,
    string MessageType,
    string PayloadJson,
    int QueueStatus,
    int RetryCount,
    DateTime LastRetryDate,
    DateTime ProcessedDate,
    string LastError,
    DateTime CrtDate)
{
    public static LabOwareOutboundQueueDto FromModel(LabOwareOutboundQueueModel model)
        => new(
            model.QueueId,
            model.OrderId,
            model.MessageType,
            model.PayloadJson,
            (int)model.QueueStatus,
            model.RetryCount,
            model.LastRetryDate,
            model.ProcessedDate,
            model.LastError,
            model.CrtDate);

    public LabOwareOutboundQueueModel ToModel()
        => LabOwareOutboundQueueModel.Rehydrate(
            QueueId,
            OrderId,
            MessageType,
            PayloadJson,
            (LabOwareQueueStatusEnum)QueueStatus,
            RetryCount,
            LastRetryDate,
            ProcessedDate,
            LastError,
            CrtDate);
}
