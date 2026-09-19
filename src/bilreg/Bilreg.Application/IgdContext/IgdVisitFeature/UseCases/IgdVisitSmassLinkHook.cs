using Bilreg.Application.IgdContext.Integration;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

/// <summary>
/// Post-commit SMASS registration-link hook shared by
/// <c>IgdVisitAssignRegisterCmd</c> and <c>IgdVisitReplaceRegisterCmd</c>
/// (architecture §5.2, §6.3, §6.6, D-09, BR-16, BR-17, AR-15, AR-16).
///
/// It runs strictly after the registration transaction has committed, performs
/// exactly one gateway attempt, records the outcome on the visit-level operational
/// <see cref="IgdVisitSmassTaskModel"/> row (<c>TaskType = Link</c>,
/// <c>NoTriage = 0</c>) in a separate write, and never lets an exception escape into
/// the register handler (BR-16). No <c>IgdEventEnum</c>/<c>AuditLog</c> entry is
/// produced (AR-16).
/// </summary>
internal static class IgdVisitSmassLinkHook
{
    /// <summary>
    /// Executes §6.6 steps 6–9. The caller is responsible for steps 1–5 (guard, load,
    /// domain behaviour, transaction, commit).
    /// </summary>
    public static async Task RunAsync(
        IIgdVisitSmassTaskRepo taskRepo,
        ISmassAssessmentGateway gateway,
        IgdVisitOptions options,
        string igdVisitId,
        RegModel reg,
        CancellationToken cancellationToken)
    {
        // §6.6 step 6 — with the toggle off no task row is created and no HTTP call is made (AR-01).
        if (!options.EnableSmassIntegration)
            return;

        // §6.6 step 7 — idempotent upsert on (IgdVisitId, 0, Link), INV-T1/INV-T2.
        IgdVisitSmassTaskModel task;
        try
        {
            var existing = taskRepo.FindByBusinessKey(
                igdVisitId, noTriage: 0, SmassTaskTypeEnum.Link);

            task = existing.HasValue
                ? existing.Value
                : IgdVisitSmassTaskModel.CreatePending(
                    igdVisitId, noTriage: 0, SmassTaskTypeEnum.Link);
        }
        catch
        {
            // The task cannot even be materialised; registration is unaffected.
            return;
        }

        // §6.6 step 8 — exactly one gateway attempt; the gateway returns failures, never
        // throws (BR-10). The link is always re-invoked because ReplaceRegister must
        // replace the latest registration values on every assessment of the visit
        // (BR-17 / AR-15); it is not skipped for an already-succeeded visit-level task.
        SmassGatewayResult result;
        try
        {
            result = await gateway.LinkIgdVisit(BuildPayload(igdVisitId, reg), cancellationToken);
        }
        catch (Exception ex)
        {
            result = new SmassGatewayResult(false, null, ex.Message);
        }

        // INV-T4/INV-T5 make a terminal Succeeded task non-transitionable. The gateway
        // call above still ran for BR-17; an already-succeeded task simply keeps its
        // recorded terminal state (no second MarkSucceeded/MarkFailed is attempted).
        if (task.TaskStatus != SmassTaskStatusEnum.Succeeded)
        {
            try
            {
                if (result.Success)
                    task.MarkSucceeded(result.AssessmentId ?? string.Empty);
                else
                    task.MarkFailed(result.ErrorMessage ?? "SMASS link gagal.");
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
        }

        // §6.6 step 9 — separate write, no transaction spanning the HTTP call.
        try
        {
            taskRepo.SaveChanges(task);
        }
        catch
        {
            // A concurrent duplicate may have won UX_BILRG_IgdVisitSmassTask_BusinessKey;
            // swallow it so registration is never affected (BR-16).
        }
    }

    /// <summary>
    /// Builds the §6.3 payload from the already-loaded <see cref="RegModel"/>.
    /// <c>IgdVisitModel.Reg</c> is only a <c>RegReff(RegId, PasienId, PasienName)</c>
    /// and does not carry <c>LayananId</c>/<c>LayananName</c>, so every value is read
    /// from the register aggregate.
    ///
    /// Shared with <c>IgdVisitSmassTaskRetryExecutor</c> so a manual Link retry
    /// rebuilds the exact same payload from the visit/Reg (AR-12).
    /// </summary>
    internal static SmassLinkIgdVisitRequest BuildPayload(string igdVisitId, RegModel reg)
        => new()
        {
            IgdVisitId = igdVisitId,
            RegId = reg.RegId,
            PasienId = reg.Pasien.PasienId,
            PasienName = reg.Pasien.PasienName,
            LayananId = reg.Layanan.LayananId,
            LayananName = reg.Layanan.LayananName
        };
}
