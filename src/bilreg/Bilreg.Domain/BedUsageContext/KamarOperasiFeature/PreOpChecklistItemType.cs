namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public record PreOpChecklistItemType(
    string ChecklistKey,
    ChecklistItemStatusEnum Status,
    string Notes = "")
{
    public const string PatientIdentity = "PATIENT_IDENTITY";
    public const string InformedConsent = "INFORMED_CONSENT";
    public const string AnesthesiaClearance = "ANESTHESIA_CLEARANCE";
    public const string PhysicalPrep = "PHYSICAL_PREP";
    public const string BloodLogistics = "BLOOD_LOGISTICS";

    public string ChecklistKey { get; init; } = ChecklistKey?.Trim() ?? string.Empty;
    public string Notes { get; init; } = Notes?.Trim() ?? string.Empty;
}
