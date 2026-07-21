using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;

public class EmrAntrianOutboundQueueModel : IEmrAntrianOutboundQueueKey
{
    private const string IdPrefix = "EAQ";
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public const string MessageTypeAddBooking = "AddBooking";
    public const string MessageTypeAddReg = "AddReg";

    private EmrAntrianOutboundQueueModel(
        string queueId,
        string sourceId,
        string messageType,
        string payloadJson,
        EmrAntrianOutboundQueueStatusEnum queueStatus,
        int retryCount,
        DateTime lastRetryDate,
        DateTime processedDate,
        string lastError,
        DateTime crtDate)
    {
        QueueId = queueId;
        SourceId = sourceId;
        MessageType = messageType;
        PayloadJson = payloadJson;
        QueueStatus = queueStatus;
        RetryCount = retryCount;
        LastRetryDate = lastRetryDate;
        ProcessedDate = processedDate;
        LastError = lastError;
        CrtDate = crtDate;
    }

    public static IEmrAntrianOutboundQueueKey Key(string queueId) => new QueueKey(queueId);

    public static EmrAntrianOutboundQueueModel CreatePending(
        string sourceId,
        string messageType,
        string payloadJson,
        DateTime createdAt = default)
    {
        Guard.Against.NullOrWhiteSpace(sourceId, nameof(sourceId));
        Guard.Against.NullOrWhiteSpace(messageType, nameof(messageType));
        Guard.Against.NullOrWhiteSpace(payloadJson, nameof(payloadJson));

        var queueId = NunaId.New(IdPrefix);
        return new EmrAntrianOutboundQueueModel(
            queueId,
            sourceId,
            messageType,
            payloadJson,
            EmrAntrianOutboundQueueStatusEnum.Pending,
            retryCount: 0,
            lastRetryDate: EmptyDate,
            processedDate: EmptyDate,
            lastError: "",
            crtDate: createdAt);
    }

    public static EmrAntrianOutboundQueueModel Rehydrate(
        string queueId,
        string sourceId,
        string messageType,
        string payloadJson,
        EmrAntrianOutboundQueueStatusEnum queueStatus,
        int retryCount,
        DateTime lastRetryDate,
        DateTime processedDate,
        string lastError,
        DateTime crtDate)
        => new(
            queueId,
            sourceId,
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
        if (QueueStatus != EmrAntrianOutboundQueueStatusEnum.Pending
            && QueueStatus != EmrAntrianOutboundQueueStatusEnum.Failed)
            throw new InvalidOperationException(
                $"Queue {QueueId} berstatus {QueueStatus}; processing hanya dari Pending atau Failed.");

        QueueStatus = EmrAntrianOutboundQueueStatusEnum.Processing;
    }

    public void MarkSucceeded(DateTime processedAt = default)
    {
        if (QueueStatus != EmrAntrianOutboundQueueStatusEnum.Processing)
            throw new InvalidOperationException(
                $"Queue {QueueId} berstatus {QueueStatus}; succeeded hanya dari Processing.");

        QueueStatus = EmrAntrianOutboundQueueStatusEnum.Succeeded;
        ProcessedDate = processedAt;
        LastError = "";
    }

    public void MarkFailed(string error, DateTime failedAt = default)
    {
        if (QueueStatus != EmrAntrianOutboundQueueStatusEnum.Processing)
            throw new InvalidOperationException(
                $"Queue {QueueId} berstatus {QueueStatus}; failed hanya dari Processing.");

        var msg = string.IsNullOrWhiteSpace(error) ? "EMR send failed" : error;
        LastError = msg.Length > 200 ? msg[..200] : msg;
        QueueStatus = EmrAntrianOutboundQueueStatusEnum.Failed;
        RetryCount++;
        LastRetryDate = failedAt;
        ProcessedDate = failedAt;
    }

    public void AssertCanManualRetry()
    {
        if (QueueStatus != EmrAntrianOutboundQueueStatusEnum.Failed)
            throw new InvalidOperationException(
                $"Queue {QueueId} berstatus {QueueStatus}; retry manual hanya diperbolehkan dari Failed.");
    }

    public string QueueId { get; init; }
    public string SourceId { get; init; }
    public string MessageType { get; init; }
    public string PayloadJson { get; init; }
    public EmrAntrianOutboundQueueStatusEnum QueueStatus { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime LastRetryDate { get; private set; }
    public DateTime ProcessedDate { get; private set; }
    public string LastError { get; private set; }
    public DateTime CrtDate { get; init; }

    private sealed record QueueKey(string QueueId) : IEmrAntrianOutboundQueueKey;
}
