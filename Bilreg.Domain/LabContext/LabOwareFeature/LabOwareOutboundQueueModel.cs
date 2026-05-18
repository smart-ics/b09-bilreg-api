using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.LabContext.LabOwareFeature;

public class LabOwareOutboundQueueModel : ILabOwareOutboundQueueKey
{
    private const string IdPrefix = "LOQ";
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public const string DefaultMessageType = "LabOrderOutbound";

    private LabOwareOutboundQueueModel(
        string queueId,
        string orderId,
        string messageType,
        string payloadJson,
        LabOwareQueueStatusEnum queueStatus,
        int retryCount,
        DateTime lastRetryDate,
        DateTime processedDate,
        string lastError,
        DateTime crtDate)
    {
        QueueId = queueId;
        OrderId = orderId;
        MessageType = messageType;
        PayloadJson = payloadJson;
        QueueStatus = queueStatus;
        RetryCount = retryCount;
        LastRetryDate = lastRetryDate;
        ProcessedDate = processedDate;
        LastError = lastError;
        CrtDate = crtDate;
    }

    public static ILabOwareOutboundQueueKey Key(string queueId) => new QueueKey(queueId);

    public static LabOwareOutboundQueueModel CreatePending(
        string orderId,
        string payloadJson,
        string? messageType = null)
    {
        Guard.Against.NullOrWhiteSpace(orderId, nameof(orderId));
        Guard.Against.NullOrWhiteSpace(payloadJson, nameof(payloadJson));

        var queueId = NunaId.New(IdPrefix);
        var now = DateTime.Now;

        return new LabOwareOutboundQueueModel(
            queueId,
            orderId,
            string.IsNullOrWhiteSpace(messageType) ? DefaultMessageType : messageType,
            payloadJson,
            LabOwareQueueStatusEnum.Pending,
            retryCount: 0,
            lastRetryDate: EmptyDate,
            processedDate: EmptyDate,
            lastError: "",
            crtDate: now);
    }

    public static LabOwareOutboundQueueModel Rehydrate(
        string queueId,
        string orderId,
        string messageType,
        string payloadJson,
        LabOwareQueueStatusEnum queueStatus,
        int retryCount,
        DateTime lastRetryDate,
        DateTime processedDate,
        string lastError,
        DateTime crtDate)
        => new(
            queueId,
            orderId,
            messageType,
            payloadJson,
            queueStatus,
            retryCount,
            lastRetryDate,
            processedDate,
            lastError,
            crtDate);

    public void MarkProcessing()
    {
        if (QueueStatus != LabOwareQueueStatusEnum.Pending
            && QueueStatus != LabOwareQueueStatusEnum.Failed)
            throw new InvalidOperationException(
                $"Queue {QueueId} berstatus {QueueStatus}; processing hanya dari Pending atau Failed.");

        QueueStatus = LabOwareQueueStatusEnum.Processing;
    }

    public void MarkSucceeded()
    {
        if (QueueStatus != LabOwareQueueStatusEnum.Processing)
            throw new InvalidOperationException(
                $"Queue {QueueId} berstatus {QueueStatus}; succeeded hanya dari Processing.");

        QueueStatus = LabOwareQueueStatusEnum.Succeeded;
        ProcessedDate = DateTime.Now;
        LastError = "";
    }

    public void MarkFailed(string error)
    {
        if (QueueStatus != LabOwareQueueStatusEnum.Processing)
            throw new InvalidOperationException(
                $"Queue {QueueId} berstatus {QueueStatus}; failed hanya dari Processing.");

        var msg = string.IsNullOrWhiteSpace(error) ? "OWARE send failed" : error;
        LastError = msg.Length > 200 ? msg[..200] : msg;
        QueueStatus = LabOwareQueueStatusEnum.Failed;
        RetryCount++;
        LastRetryDate = DateTime.Now;
        ProcessedDate = DateTime.Now;
    }

    public void AssertCanManualRetry()
    {
        if (QueueStatus != LabOwareQueueStatusEnum.Failed)
            throw new InvalidOperationException(
                $"Queue {QueueId} berstatus {QueueStatus}; retry manual hanya diperbolehkan dari Failed.");
    }

    public string QueueId { get; init; }
    public string OrderId { get; init; }
    public string MessageType { get; init; }
    public string PayloadJson { get; init; }
    public LabOwareQueueStatusEnum QueueStatus { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime LastRetryDate { get; private set; }
    public DateTime ProcessedDate { get; private set; }
    public string LastError { get; private set; }
    public DateTime CrtDate { get; init; }

    private sealed record QueueKey(string QueueId) : ILabOwareOutboundQueueKey;
}
