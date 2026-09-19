using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;

/// <summary>
/// Operational record of one outbound SMASS operation for one IGD triage event
/// (<see cref="SmassTaskTypeEnum.Generate"/>) or one visit-level link operation
/// (<see cref="SmassTaskTypeEnum.Link"/>). It is a worklist entry, never a
/// synchronization mechanism, and it is never deleted (INV-T8 / D-15).
/// </summary>
public class IgdVisitSmassTaskModel : IIgdVisitSmassTaskKey
{
    private const string IdPrefix = "IST";
    private const int MaxErrorLength = 500;
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    private IgdVisitSmassTaskModel(
        string igdVisitSmassTaskId,
        string igdVisitId,
        int noTriage,
        SmassTaskTypeEnum taskType,
        SmassTaskStatusEnum taskStatus,
        string assessmentId,
        int retryCount,
        DateTime lastRetryDate,
        DateTime processedDate,
        string lastError,
        DateTime crtDate)
    {
        ValidateBusinessKey(noTriage, taskType);

        IgdVisitSmassTaskId = igdVisitSmassTaskId;
        IgdVisitId = igdVisitId;
        NoTriage = noTriage;
        TaskType = taskType;
        TaskStatus = taskStatus;
        AssessmentId = assessmentId;
        RetryCount = retryCount;
        LastRetryDate = lastRetryDate;
        ProcessedDate = processedDate;
        LastError = lastError;
        CrtDate = crtDate;
    }

    public static IIgdVisitSmassTaskKey Key(string igdVisitSmassTaskId)
        => new TaskKey(igdVisitSmassTaskId);

    /// <summary>
    /// Creates a brand new <see cref="SmassTaskStatusEnum.Pending"/> task.
    /// INV-T1 (one task per business key) is enforced by
    /// UX_BILRG_IgdVisitSmassTask_BusinessKey together with the upsert path
    /// (<c>FindByBusinessKey</c> + <c>SaveChanges</c>).
    /// </summary>
    public static IgdVisitSmassTaskModel CreatePending(
        string igdVisitId,
        int noTriage,
        SmassTaskTypeEnum taskType,
        DateTime createdAt = default)
    {
        Guard.Against.NullOrWhiteSpace(igdVisitId, nameof(igdVisitId));

        return new IgdVisitSmassTaskModel(
            NunaId.New(IdPrefix),
            igdVisitId,
            noTriage,
            taskType,
            SmassTaskStatusEnum.Pending,
            assessmentId: "",
            retryCount: 0,
            lastRetryDate: EmptyDate,
            processedDate: EmptyDate,
            lastError: "",
            crtDate: createdAt == default ? DateTime.Now : createdAt);
    }

    public static IgdVisitSmassTaskModel Rehydrate(
        string igdVisitSmassTaskId,
        string igdVisitId,
        int noTriage,
        SmassTaskTypeEnum taskType,
        SmassTaskStatusEnum taskStatus,
        string assessmentId,
        int retryCount,
        DateTime lastRetryDate,
        DateTime processedDate,
        string lastError,
        DateTime crtDate)
        => new(
            igdVisitSmassTaskId,
            igdVisitId,
            noTriage,
            taskType,
            taskStatus,
            assessmentId,
            retryCount,
            lastRetryDate,
            processedDate,
            lastError,
            crtDate);

    /// <summary>
    /// Marks the task succeeded. Legal only from Pending or Failed (INV-T4).
    /// A succeeded Generate task must carry a non-empty AssessmentId (INV-T7).
    /// </summary>
    public void MarkSucceeded(string assessmentId, DateTime processedAt = default)
    {
        if (TaskStatus != SmassTaskStatusEnum.Pending
            && TaskStatus != SmassTaskStatusEnum.Failed)
            throw new InvalidOperationException(
                $"Task {IgdVisitSmassTaskId} berstatus {TaskStatus}; succeeded hanya dari Pending atau Failed.");

        var id = assessmentId ?? "";
        if (TaskType == SmassTaskTypeEnum.Generate && string.IsNullOrWhiteSpace(id))
            throw new ArgumentException(
                $"Task {IgdVisitSmassTaskId} bertipe Generate; AssessmentId wajib diisi saat succeeded.",
                nameof(assessmentId));

        TaskStatus = SmassTaskStatusEnum.Succeeded;
        AssessmentId = id;
        LastError = "";
        ProcessedDate = processedAt == default ? DateTime.Now : processedAt;
    }

    /// <summary>
    /// Marks the task failed. Legal only from Pending or Failed (INV-T5).
    /// LastError is truncated to 500 characters.
    /// </summary>
    public void MarkFailed(string error, DateTime failedAt = default)
    {
        if (TaskStatus != SmassTaskStatusEnum.Pending
            && TaskStatus != SmassTaskStatusEnum.Failed)
            throw new InvalidOperationException(
                $"Task {IgdVisitSmassTaskId} berstatus {TaskStatus}; failed hanya dari Pending atau Failed.");

        var msg = string.IsNullOrWhiteSpace(error) ? "SMASS operation failed" : error.Trim();
        LastError = msg.Length > MaxErrorLength ? msg[..MaxErrorLength] : msg;

        var at = failedAt == default ? DateTime.Now : failedAt;
        TaskStatus = SmassTaskStatusEnum.Failed;
        RetryCount++;
        LastRetryDate = at;
        ProcessedDate = at;
    }

    /// <summary>
    /// Manual retry is only allowed from Failed (INV-T6).
    /// </summary>
    public void AssertCanManualRetry()
    {
        if (TaskStatus != SmassTaskStatusEnum.Failed)
            throw new InvalidOperationException(
                $"Task {IgdVisitSmassTaskId} berstatus {TaskStatus}; retry manual hanya diperbolehkan dari Failed.");
    }

    public string IgdVisitSmassTaskId { get; }
    public string IgdVisitId { get; }
    public int NoTriage { get; }
    public SmassTaskTypeEnum TaskType { get; }
    public SmassTaskStatusEnum TaskStatus { get; private set; }
    public string AssessmentId { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime LastRetryDate { get; private set; }
    public DateTime ProcessedDate { get; private set; }
    public string LastError { get; private set; }
    public DateTime CrtDate { get; }

    private static void ValidateBusinessKey(int noTriage, SmassTaskTypeEnum taskType)
    {
        if (taskType == SmassTaskTypeEnum.Link && noTriage != 0)
            throw new ArgumentException(
                "Task Link mensyaratkan NoTriage = 0 (INV-T2).", nameof(noTriage));

        if (taskType == SmassTaskTypeEnum.Generate && noTriage <= 0)
            throw new ArgumentException(
                "Task Generate mensyaratkan NoTriage > 0 (INV-T3).", nameof(noTriage));
    }

    private sealed record TaskKey(string IgdVisitSmassTaskId) : IIgdVisitSmassTaskKey;
}
