using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;

namespace Bilreg.Application.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// Applies currently implemented command dependencies to B1 candidate actions.
/// Does not invent unimplemented blockers (bed occupancy, medication, clinical docs, ward transfer).
/// </summary>
public static class JourneyAllowedActionEvaluator
{
    public const string RegistrationHasBillingItemsBlockedReason =
        "Registration memiliki item tagihan Tata Rekening; pembatalan admisi tidak dapat dijalankan.";

    /// <summary>
    /// Evaluates final allowed actions from B1 candidates using billing eligibility for CancelAdmission.
    /// </summary>
    /// <param name="candidates">Stage/ownership candidates from the B1 resolver.</param>
    /// <param name="regId">Primary registration id when the journey is registered; null/empty for prospective.</param>
    /// <param name="hasBillingItems">
    /// Authoritative Tata Rekening check for <paramref name="regId"/>.
    /// When <paramref name="regId"/> is absent, CancelAdmission is not made executable.
    /// </param>
    public static IReadOnlyList<JourneyAllowedAction> Evaluate(
        IReadOnlyList<JourneyAllowedAction> candidates,
        string? regId,
        Func<string, bool>? hasBillingItems)
    {
        if (candidates.Count == 0)
            return candidates;

        var result = new List<JourneyAllowedAction>(candidates.Count);
        foreach (var action in candidates)
        {
            if (action.Code != JourneyActionCode.CancelAdmission)
            {
                result.Add(action);
                continue;
            }

            if (string.IsNullOrWhiteSpace(regId) || hasBillingItems is null)
            {
                result.Add(action with
                {
                    CanExecute = false,
                    BlockedReason = "Eligibilitas pembatalan memerlukan evaluasi dependensi perintah (B3/detail)."
                });
                continue;
            }

            if (hasBillingItems(regId))
            {
                result.Add(action with
                {
                    CanExecute = false,
                    BlockedReason = RegistrationHasBillingItemsBlockedReason
                });
                continue;
            }

            result.Add(action with
            {
                CanExecute = true,
                BlockedReason = null
            });
        }

        return result;
    }

    public static IReadOnlyList<JourneyAllowedAction> Evaluate(
        IReadOnlyList<JourneyAllowedAction> candidates,
        string? regId,
        IRegistrationCancellationEligibilityRepo? eligibility) =>
        Evaluate(
            candidates,
            regId,
            eligibility is null ? null : eligibility.HasBillingItems);
}
