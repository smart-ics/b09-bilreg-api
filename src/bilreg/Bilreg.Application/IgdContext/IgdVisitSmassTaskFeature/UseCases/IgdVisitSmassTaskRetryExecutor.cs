using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature.UseCases;

/// <summary>
/// Shared manual-retry semantics for one operational <see cref="IgdVisitSmassTaskModel"/>
/// (architecture §5.2, §6.2, §6.3, D-07). Both <c>IgdVisitSmassTaskRetryCmd</c> (single
/// task) and <c>IgdVisitSmassTaskProcessCmd</c> (operator worklist batch) execute this
/// one implementation, so both paths rebuild the payload identically.
///
/// No payload is persisted or replayed (AR-12): a Generate payload is rebuilt from the
/// immutable <c>BILRG_IgdVisitTriage</c> row reached through the visit aggregate, and a
/// Link payload is rebuilt from the visit / loaded <c>RegModel</c>. The gateway is
/// invoked exactly once and failures are returned, never thrown (BR-10).
/// </summary>
internal static class IgdVisitSmassTaskRetryExecutor
{
    /// <summary>
    /// Rebuilds the payload from immutable sources, performs one gateway attempt and
    /// records the outcome on the task row (separate write, no transaction across HTTP).
    /// If Link task succeeds, sequentially registers clinical labels in EMR 2.0 (TD-06).
    /// The caller owns the task's lifecycle and any surrounding loop.
    /// </summary>
    public static async Task ExecuteAsync(
        IIgdVisitSmassTaskRepo taskRepo,
        IIgdVisitRepo igdVisitRepo,
        IRegRepo regRepo,
        ISmassAssessmentGateway gateway,
        IEmrLabelGateway emrLabelGateway,
        IgdVisitOptions options,
        IgdVisitSmassTaskModel task,
        string userId,
        CancellationToken cancellationToken)
    {
        // INV-T6 / IR-01 — manual retry is only legal from Failed. Re-asserted here so
        // the batch path enforces the same transition guard as the single-task path.
        task.AssertCanManualRetry();

        RegModel? reg = null;
        SmassGatewayResult result;
        try
        {
            if (task.TaskType == SmassTaskTypeEnum.Generate)
            {
                result = await gateway.GenerateIgdTriage(
                    BuildGeneratePayload(igdVisitRepo, options, task), cancellationToken);
            }
            else
            {
                var (linkPayload, loadedReg) = BuildLinkPayload(igdVisitRepo, regRepo, task);
                reg = loadedReg;
                result = await gateway.LinkIgdVisit(linkPayload, cancellationToken);
            }
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
                task.MarkFailed(result.ErrorMessage ?? $"SMASS {task.TaskType.ToCode()} retry gagal.");
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

        // Separate write; no TransHelper scope spans the HTTP call (architecture §6.6).
        taskRepo.SaveChanges(task);

        // Architecture TD-04 / TD-06: Post-link EMR 2.0 label registration on manual retry.
        // Sequential iteration for each linked assessment. Any failure is swallowed to isolate errors (TD-05).
        if (task.TaskType == SmassTaskTypeEnum.Link &&
            result.Success &&
            emrLabelGateway != null &&
            reg != null &&
            result.LinkedAssessmentIds is { Count: > 0 })
        {
            var effectiveUserId = !string.IsNullOrWhiteSpace(userId)
                ? userId
                : reg.RegMasukAudit?.UserId ?? string.Empty;

            foreach (var assessmentId in result.LinkedAssessmentIds)
            {
                try
                {
                    var labelRequest = new EmrAddSmassLabelRequest
                    {
                        AssesmentId = assessmentId,
                        LayananId = !string.IsNullOrWhiteSpace(reg.Layanan?.LayananId)
                            ? reg.Layanan.LayananId
                            : options.SmassLayananId,
                        PaperId = options.SmassTriagePaperId,
                        PaperName = options.SmassTriagePaperName,
                        RegId = reg.RegId,
                        UserrId = effectiveUserId
                    };
                    await emrLabelGateway.AddSmassLabel(labelRequest, cancellationToken);
                }
                catch
                {
                    // Swallow EMR exceptions so retry SMASS task status remains unaffected (TD-05).
                }
            }
        }
    }

    /// <summary>
    /// Backwards-compatible overload for callers that do not supply IEmrLabelGateway or userId.
    /// </summary>
    public static Task ExecuteAsync(
        IIgdVisitSmassTaskRepo taskRepo,
        IIgdVisitRepo igdVisitRepo,
        IRegRepo regRepo,
        ISmassAssessmentGateway gateway,
        IgdVisitOptions options,
        IgdVisitSmassTaskModel task,
        CancellationToken cancellationToken)
        => ExecuteAsync(taskRepo, igdVisitRepo, regRepo, gateway, null!, options, task, string.Empty, cancellationToken);

    /// <summary>
    /// Rebuilds the Generate payload from the immutable triage row selected by
    /// (<c>IgdVisitId</c>, <c>NoTriage</c>) through the already-persisted visit aggregate
    /// (DR-02, AR-12). Uses the visit repo read path; no ad-hoc SQL is introduced.
    /// </summary>
    private static SmassGenerateIgdTriageRequest BuildGeneratePayload(
        IIgdVisitRepo igdVisitRepo,
        IgdVisitOptions options,
        IgdVisitSmassTaskModel task)
    {
        var visit = igdVisitRepo.LoadEntity(IgdVisitModel.Key(task.IgdVisitId))
            .GetValueOrThrow($"IgdVisit '{task.IgdVisitId}' not found");

        var triage = visit.ListTriage.FirstOrDefault(x => x.NoTriage == task.NoTriage)
            ?? throw new InvalidOperationException(
                $"Triage NoTriage {task.NoTriage} pada IgdVisit '{task.IgdVisitId}' tidak ditemukan; " +
                "payload generate tidak dapat dibangun ulang.");

        return IgdVisitSmassGenerationHook.BuildPayload(task.IgdVisitId, triage, options);
    }

    /// <summary>
    /// Rebuilds the Link payload from the visit and its administrative registration
    /// (architecture §6.3). The visit aggregate only carries
    /// <c>RegReff(RegId, PasienId, PasienName)</c>, so the full <see cref="RegModel"/> is
    /// loaded by <c>RegId</c> for <c>LayananId</c>/<c>LayananName</c>.
    /// </summary>
    private static (SmassLinkIgdVisitRequest Request, RegModel Reg) BuildLinkPayload(
        IIgdVisitRepo igdVisitRepo,
        IRegRepo regRepo,
        IgdVisitSmassTaskModel task)
    {
        var visit = igdVisitRepo.LoadEntity(IgdVisitModel.Key(task.IgdVisitId))
            .GetValueOrThrow($"IgdVisit '{task.IgdVisitId}' not found");

        if (!visit.HasReg)
            throw new InvalidOperationException(
                $"IgdVisit '{task.IgdVisitId}' belum memiliki registrasi; " +
                "payload link tidak dapat dibangun ulang.");

        var reg = regRepo.LoadEntity(RegModel.Key(visit.Reg.RegId))
            .GetValueOrThrow($"Reg '{visit.Reg.RegId}' not found");

        return (IgdVisitSmassLinkHook.BuildPayload(task.IgdVisitId, reg), reg);
    }
}
