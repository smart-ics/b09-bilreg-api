using Bilreg.Application.IgdContext.Integration;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

/// <summary>
/// Post-commit SMASS void hook invoked by <c>IgdVisitVoidTriageCmd</c> (architecture TD-03, §5, §6, §9).
///
/// Runs strictly after the void-triage DB transaction has committed, looks up the
/// corresponding <see cref="SmassTaskTypeEnum.Generate"/> task for <c>(igdVisitId, noTriage)</c>
/// to obtain the SMASS <c>AssessmentId</c>, upserts an idempotent <see cref="SmassTaskTypeEnum.Void"/>
/// task row, executes one <see cref="ISmassAssessmentGateway.DeleteAssesment"/> attempt,
/// persists the task outcome in a separate write, and never lets an exception escape
/// into the void-triage handler.
/// </summary>
internal static class IgdVisitSmassVoidHook
{
    public static async Task<SmassVoidResult> RunAsync(
        IIgdVisitSmassTaskRepo taskRepo,
        ISmassAssessmentGateway gateway,
        IgdVisitOptions options,
        string igdVisitId,
        int noTriage,
        CancellationToken cancellationToken)
    {
        // Step 1: feature toggle off → Disabled, no repo/HTTP call.
        if (!options.EnableSmassIntegration)
            return SmassVoidResult.Disabled();

        // Step 2: locate Generate task for (igdVisitId, noTriage) to get AssessmentId.
        string assessmentId;
        try
        {
            var generateTask = taskRepo.FindByBusinessKey(
                igdVisitId, noTriage, SmassTaskTypeEnum.Generate);

            if (!generateTask.HasValue || string.IsNullOrWhiteSpace(generateTask.Value.AssessmentId))
                return SmassVoidResult.Skipped();

            assessmentId = generateTask.Value.AssessmentId.Trim();
        }
        catch
        {
            return SmassVoidResult.Pending();
        }

        // Step 3: idempotent upsert on (igdVisitId, noTriage, Void) storing AssessmentId.
        IgdVisitSmassTaskModel task;
        try
        {
            var existing = taskRepo.FindByBusinessKey(
                igdVisitId, noTriage, SmassTaskTypeEnum.Void);

            if (existing.HasValue)
            {
                task = existing.Value;
                if (task.TaskStatus != SmassTaskStatusEnum.Succeeded
                    && string.IsNullOrWhiteSpace(task.AssessmentId))
                {
                    task.SetAssessmentId(assessmentId);
                }
            }
            else
            {
                task = IgdVisitSmassTaskModel.CreatePending(
                    igdVisitId, noTriage, SmassTaskTypeEnum.Void, assessmentId: assessmentId);
            }
        }
        catch
        {
            return SmassVoidResult.Pending(assessmentId);
        }

        // Already succeeded: return Voided without redundant HTTP call.
        if (task.TaskStatus == SmassTaskStatusEnum.Succeeded)
            return SmassVoidResult.Voided(
                string.IsNullOrWhiteSpace(task.AssessmentId) ? assessmentId : task.AssessmentId);

        var targetAssessmentId = string.IsNullOrWhiteSpace(task.AssessmentId)
            ? assessmentId
            : task.AssessmentId;

        // Step 4: execute DeleteAssesment once; isolate all exceptions.
        SmassGatewayResult result;
        try
        {
            result = await gateway.DeleteAssesment(targetAssessmentId, cancellationToken);
        }
        catch (Exception ex)
        {
            result = new SmassGatewayResult(false, null, ex.Message);
        }

        // Step 5: record outcome on task.
        try
        {
            if (result.Success)
            {
                task.MarkSucceeded(
                    string.IsNullOrWhiteSpace(result.AssessmentId)
                        ? targetAssessmentId
                        : result.AssessmentId);
            }
            else
            {
                task.MarkFailed(result.ErrorMessage ?? "SMASS deleteAssesment gagal.");
            }
        }
        catch (Exception ex)
        {
            try
            {
                task.MarkFailed(ex.Message);
            }
            catch
            {
                // Best-effort in-memory state.
            }
        }

        // Step 6: save task in separate write without throwing.
        try
        {
            taskRepo.SaveChanges(task);
        }
        catch
        {
            // A concurrent duplicate may have won UX_BILRG_IgdVisitSmassTask_BusinessKey;
            // re-read the business key and treat the persisted row as authoritative.
            try
            {
                var persisted = taskRepo.FindByBusinessKey(
                    igdVisitId, noTriage, SmassTaskTypeEnum.Void);
                if (persisted.HasValue)
                    return SmassVoidResult.FromTask(persisted.Value);
            }
            catch
            {
                // ignore — no persisted terminal result is available.
            }

            return SmassVoidResult.Pending(targetAssessmentId);
        }

        return SmassVoidResult.FromTask(task);
    }
}

/// <summary>
/// Result of one post-commit SMASS void hook. <see cref="Status"/> uses the exact
/// approved values <c>"Disabled" | "Skipped" | "Pending" | "Voided" | "Failed"</c>
/// (architecture TD-01, TD-03).
/// </summary>
internal readonly record struct SmassVoidResult(string AssessmentId, string Status)
{
    public static SmassVoidResult Disabled() => new(string.Empty, "Disabled");

    public static SmassVoidResult Skipped() => new(string.Empty, "Skipped");

    public static SmassVoidResult Pending(string? assessmentId = null)
        => new(assessmentId ?? string.Empty, "Pending");

    public static SmassVoidResult Failed(string? assessmentId = null)
        => new(assessmentId ?? string.Empty, "Failed");

    public static SmassVoidResult Voided(string? assessmentId)
        => new(assessmentId ?? string.Empty, "Voided");

    public static SmassVoidResult FromTask(IgdVisitSmassTaskModel task)
        => task.TaskStatus switch
        {
            SmassTaskStatusEnum.Succeeded => Voided(task.AssessmentId),
            SmassTaskStatusEnum.Failed => Failed(task.AssessmentId),
            _ => Pending(task.AssessmentId)
        };
}
