namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature;

/// <summary>
/// Read-only Tata Rekening gate for coordinated Rawat Inap Registration cancellation.
/// Other cancellation state validation remains the orchestrator's responsibility.
/// </summary>
public interface IRegistrationCancellationEligibilityRepo
{
    bool HasBillingItems(string regId);
}

public static class RegistrationCancellationBlockerCode
{
    public const string HasBillingItems = "REGISTRATION_HAS_BILLING_ITEMS";
}
