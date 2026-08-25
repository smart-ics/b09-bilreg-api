using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ApotekContext.IntegrationFeature;

public class AptIntegrationTaskModel : IAptIntegrationTaskKey
{
    public const string IdPrefix = "AIT";
    public const int MaxRetries = 5;
    public const int StaleProcessingMinutes = 30;
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    private AptIntegrationTaskModel(
        string integrationTaskId,
        AptIntegrationTaskTypeEnum taskType,
        AptIntegrationSourceKindEnum sourceKind,
        string sourceId,
        string idempotencyKey,
        AptIntegrationDestinationEnum destination,
        string payloadJson,
        AptIntegrationTaskStatusEnum taskStatus,
        int retryCount,
        string lastError,
        DateTime lastRetryDate,
        DateTime processedDate,
        string correlationId,
        DateTime crtDate)
    {
        IntegrationTaskId = integrationTaskId;
        TaskType = taskType;
        SourceKind = sourceKind;
        SourceId = sourceId;
        IdempotencyKey = idempotencyKey;
        Destination = destination;
        PayloadJson = payloadJson;
        TaskStatus = taskStatus;
        RetryCount = retryCount;
        LastError = lastError;
        LastRetryDate = lastRetryDate;
        ProcessedDate = processedDate;
        CorrelationId = correlationId;
        CrtDate = crtDate;
    }

    public static IAptIntegrationTaskKey Key(string integrationTaskId)
        => new TaskKey(integrationTaskId);

    public static AptIntegrationTaskModel CreatePending(
        AptIntegrationTaskTypeEnum taskType,
        AptIntegrationSourceKindEnum sourceKind,
        string sourceId,
        string idempotencyKey,
        AptIntegrationDestinationEnum destination,
        string payloadJson,
        DateTime createdAt = default)
    {
        Guard.Against.NullOrWhiteSpace(sourceId, nameof(sourceId));
        Guard.Against.NullOrWhiteSpace(idempotencyKey, nameof(idempotencyKey));
        Guard.Against.NullOrWhiteSpace(payloadJson, nameof(payloadJson));
        if (idempotencyKey.Length > 80)
            throw new ArgumentOutOfRangeException(nameof(idempotencyKey), "IdempotencyKey max length is 80.");

        return new AptIntegrationTaskModel(
            NunaId.New(IdPrefix),
            taskType,
            sourceKind,
            sourceId,
            idempotencyKey,
            destination,
            payloadJson,
            AptIntegrationTaskStatusEnum.Pending,
            retryCount: 0,
            lastError: "",
            lastRetryDate: EmptyDate,
            processedDate: EmptyDate,
            correlationId: "",
            crtDate: createdAt == default ? DateTime.Now : createdAt);
    }

    public static AptIntegrationTaskModel Rehydrate(
        string integrationTaskId,
        AptIntegrationTaskTypeEnum taskType,
        AptIntegrationSourceKindEnum sourceKind,
        string sourceId,
        string idempotencyKey,
        AptIntegrationDestinationEnum destination,
        string payloadJson,
        AptIntegrationTaskStatusEnum taskStatus,
        int retryCount,
        string lastError,
        DateTime lastRetryDate,
        DateTime processedDate,
        string correlationId,
        DateTime crtDate)
        => new(
            integrationTaskId,
            taskType,
            sourceKind,
            sourceId,
            idempotencyKey,
            destination,
            payloadJson,
            taskStatus,
            retryCount,
            lastError,
            lastRetryDate,
            processedDate,
            correlationId,
            crtDate);

    public void ClaimPending()
    {
        if (TaskStatus != AptIntegrationTaskStatusEnum.Pending)
            throw new InvalidOperationException(
                $"Task {IntegrationTaskId} berstatus {TaskStatus}; claim hanya dari Pending.");

        TaskStatus = AptIntegrationTaskStatusEnum.Processing;
        ProcessedDate = DateTime.Now;
    }

    public void MarkSucceeded(string correlationId, DateTime processedAt = default)
    {
        if (TaskStatus != AptIntegrationTaskStatusEnum.Processing)
            throw new InvalidOperationException(
                $"Task {IntegrationTaskId} berstatus {TaskStatus}; succeeded hanya dari Processing.");

        TaskStatus = AptIntegrationTaskStatusEnum.Succeeded;
        CorrelationId = correlationId ?? "";
        LastError = "";
        ProcessedDate = processedAt == default ? DateTime.Now : processedAt;
    }

    public void MarkFailed(string error, DateTime failedAt = default)
    {
        if (TaskStatus != AptIntegrationTaskStatusEnum.Processing)
            throw new InvalidOperationException(
                $"Task {IntegrationTaskId} berstatus {TaskStatus}; failed hanya dari Processing.");

        var msg = string.IsNullOrWhiteSpace(error) ? "Integration task failed" : error;
        LastError = msg.Length > 200 ? msg[..200] : msg;
        RetryCount++;
        LastRetryDate = failedAt == default ? DateTime.Now : failedAt;
        ProcessedDate = LastRetryDate;
        TaskStatus = RetryCount >= MaxRetries
            ? AptIntegrationTaskStatusEnum.Dead
            : AptIntegrationTaskStatusEnum.Failed;
    }

    public void AssertCanRetry()
    {
        if (TaskStatus != AptIntegrationTaskStatusEnum.Failed)
            throw new InvalidOperationException(
                $"Task {IntegrationTaskId} berstatus {TaskStatus}; retry hanya dari Failed.");
    }

    public void PrepareRetry()
    {
        AssertCanRetry();
        TaskStatus = AptIntegrationTaskStatusEnum.Pending;
    }

    public bool IsStaleProcessing(DateTime asOf)
        => TaskStatus == AptIntegrationTaskStatusEnum.Processing
           && ProcessedDate != EmptyDate
           && (asOf - ProcessedDate).TotalMinutes >= StaleProcessingMinutes;

    public void ReclaimStaleProcessing(DateTime reclaimedAt = default)
    {
        var asOf = reclaimedAt == default ? DateTime.Now : reclaimedAt;
        if (!IsStaleProcessing(asOf))
            throw new InvalidOperationException(
                $"Task {IntegrationTaskId} berstatus {TaskStatus}; reclaim hanya untuk Processing stale >= {StaleProcessingMinutes} menit.");

        TaskStatus = AptIntegrationTaskStatusEnum.Pending;
        LastError = "reclaimed from stale processing";
    }

    public string IntegrationTaskId { get; }
    public AptIntegrationTaskTypeEnum TaskType { get; }
    public AptIntegrationSourceKindEnum SourceKind { get; }
    public string SourceId { get; }
    public string IdempotencyKey { get; }
    public AptIntegrationDestinationEnum Destination { get; }
    public string PayloadJson { get; }
    public AptIntegrationTaskStatusEnum TaskStatus { get; private set; }
    public int RetryCount { get; private set; }
    public string LastError { get; private set; }
    public DateTime LastRetryDate { get; private set; }
    public DateTime ProcessedDate { get; private set; }
    public string CorrelationId { get; private set; }
    public DateTime CrtDate { get; }

    private sealed record TaskKey(string IntegrationTaskId) : IAptIntegrationTaskKey;
}
