using Bilreg.Application.IgdContext.Integration;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

/// <summary>
/// Post-commit SMASS generation hook shared by <c>IgdVisitAssessTriageCmd</c> and
/// <c>IgdVisitReAssessTriageCmd</c> (architecture §5.2, §6.2, §6.6, AR-01, AR-12,
/// AR-16, BR-10).
///
/// It runs strictly after the triage transaction has committed, performs exactly
/// one gateway attempt, records the outcome on the operational
/// <see cref="IgdVisitSmassTaskModel"/> row in a separate write, and never lets an
/// exception escape into the triage handler. No <c>IgdEventEnum</c>/<c>AuditLog</c>
/// entry is produced (AR-16).
/// </summary>
internal static class IgdVisitSmassGenerationHook
{
    /// <summary>
    /// Executes §6.6 steps 6–9. The caller is responsible for steps 1–5 (guard, load,
    /// domain behaviour, transaction, commit).
    /// </summary>
    public static async Task<SmassGenerationResult> RunAsync(
        IIgdVisitSmassTaskRepo taskRepo,
        ISmassAssessmentGateway gateway,
        IgdVisitOptions options,
        string igdVisitId,
        IgdVisitTriageType triage,
        CancellationToken cancellationToken)
    {
        // §6.6 step 6 — with the toggle off no task row is created and no HTTP call is made (AR-01).
        if (!options.EnableSmassIntegration)
            return SmassGenerationResult.Disabled();

        // §6.6 step 7 — idempotent upsert on (IgdVisitId, NoTriage, Generate), INV-T1.
        IgdVisitSmassTaskModel task;
        try
        {
            var existing = taskRepo.FindByBusinessKey(
                igdVisitId, triage.NoTriage, SmassTaskTypeEnum.Generate);

            task = existing.HasValue
                ? existing.Value
                : IgdVisitSmassTaskModel.CreatePending(
                    igdVisitId, triage.NoTriage, SmassTaskTypeEnum.Generate);
        }
        catch
        {
            // The task cannot even be materialised, so no terminal result can be recorded.
            return SmassGenerationResult.Pending();
        }

        // A concurrent duplicate already produced an assessment; re-use it (INV-T4 forbids
        // a second MarkSucceeded) and never issue a redundant HTTP call.
        if (task.TaskStatus == SmassTaskStatusEnum.Succeeded)
            return SmassGenerationResult.Generated(task.AssessmentId);

        // §6.6 step 8 — exactly one gateway attempt; the gateway returns failures, never throws (BR-10).
        SmassGatewayResult result;
        try
        {
            result = await gateway.GenerateIgdTriage(
                BuildPayload(igdVisitId, triage, options), cancellationToken);
        }
        catch (Exception ex)
        {
            result = new SmassGatewayResult(false, null, ex.Message);
        }

        try
        {
            if (result.Success)
                task.MarkSucceeded(result.AssessmentId ?? string.Empty);
            else
                task.MarkFailed(result.ErrorMessage ?? "SMASS generate gagal.");
        }
        catch (Exception ex)
        {
            try
            {
                task.MarkFailed(ex.Message);
            }
            catch
            {
                // MarkFailed is legal from Pending/Failed only; best-effort in-memory state.
            }
        }

        // §6.6 step 9 — separate write, no transaction spanning the HTTP call.
        try
        {
            taskRepo.SaveChanges(task);
        }
        catch
        {
            // A concurrent duplicate may have won UX_BILRG_IgdVisitSmassTask_BusinessKey;
            // re-read the business key and treat the persisted row as authoritative (§6.2).
            try
            {
                var persisted = taskRepo.FindByBusinessKey(
                    igdVisitId, triage.NoTriage, SmassTaskTypeEnum.Generate);
                if (persisted.HasValue)
                    return SmassGenerationResult.FromTask(persisted.Value);
            }
            catch
            {
                // ignore — no persisted terminal result is available.
            }

            return SmassGenerationResult.Pending();
        }

        return SmassGenerationResult.FromTask(task);
    }

    /// <summary>
    /// Builds the §5.3/§6.2 payload from the committed triage row. <c>PaperId</c> and
    /// <c>LayananId</c> come from configuration (D-05, D-06/AR-02); the gateway also
    /// stamps them onto the wire payload (single source of truth).
    ///
    /// Shared with <c>IgdVisitSmassTaskRetryExecutor</c> so a manual Generate retry
    /// rebuilds the exact same payload from the immutable triage row (AR-12).
    /// </summary>
    internal static SmassGenerateIgdTriageRequest BuildPayload(
        string igdVisitId,
        IgdVisitTriageType triage,
        IgdVisitOptions options)
        => new()
        {
            IgdVisitId = igdVisitId,
            NoTriage = triage.NoTriage,
            PaperId = options.SmassTriagePaperId,
            LayananId = options.SmassLayananId,
            UserrId = triage.AssessorUserId,
            AssesmentDate = triage.AssessmentDateTime.ToString("yyyy-MM-dd"),
            AssesmentTime = triage.AssessmentDateTime.ToString("HH:mm:ss"),
            AirwaysScore = triage.AirwaysScore,
            BreathingScore = triage.BreathingScore,
            BloodCirculationScore = triage.BloodCirculationScore,
            GcsEyeScore = triage.GcsEyeScore,
            GcsMotorScore = triage.GcsMotorScore,
            GcsVoiceScore = triage.GcsVoiceScore,
            AtsLevel = triage.Level.ToCode(),
            TriageColor = triage.Color.ToCode(),
            IsManualOverrideBlack = triage.IsManualOverrideBlack
        };
}

/// <summary>
/// Result of one post-commit generation hook. <see cref="Status"/> uses the exact
/// approved values <c>"Disabled" | "Pending" | "Generated" | "Failed"</c>
/// (architecture §5.2).
/// </summary>
internal readonly record struct SmassGenerationResult(string AssessmentId, string Status)
{
    public static SmassGenerationResult Disabled() => new(string.Empty, "Disabled");

    public static SmassGenerationResult Pending() => new(string.Empty, "Pending");

    public static SmassGenerationResult Failed() => new(string.Empty, "Failed");

    public static SmassGenerationResult Generated(string? assessmentId)
        => new(assessmentId ?? string.Empty, "Generated");

    public static SmassGenerationResult FromTask(IgdVisitSmassTaskModel task)
        => task.TaskStatus switch
        {
            SmassTaskStatusEnum.Succeeded => Generated(task.AssessmentId),
            SmassTaskStatusEnum.Failed => Failed(),
            _ => Pending()
        };
}
