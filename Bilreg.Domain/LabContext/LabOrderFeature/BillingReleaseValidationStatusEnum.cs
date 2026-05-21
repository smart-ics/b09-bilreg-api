namespace Bilreg.Domain.LabContext.LabOrderFeature;

/// <summary>
/// Non-authoritative snapshot of the last BIL release-eligibility check (audit trace only).
/// </summary>
public enum BillingReleaseValidationStatusEnum
{
    NotChecked = 0,
    Clear = 1,
    Blocked = 2
}
