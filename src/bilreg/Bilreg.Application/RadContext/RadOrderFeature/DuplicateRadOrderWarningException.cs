namespace Bilreg.Application.RadContext.RadOrderFeature;

public class DuplicateRadOrderWarningException : InvalidOperationException
{
    public const string WarningCode = "RAD_DUPLICATE_ORDER_WARNING";

    public string PatientId { get; }
    public string Modality { get; }
    public IReadOnlyList<string> DuplicateTarifIds { get; }

    public DuplicateRadOrderWarningException(
        string message,
        string patientId = "",
        string modality = "",
        IEnumerable<string>? duplicateTarifIds = null)
        : base(message)
    {
        PatientId = patientId ?? string.Empty;
        Modality = modality ?? string.Empty;
        DuplicateTarifIds = duplicateTarifIds?.ToList() ?? [];
    }

    public DuplicateRadOrderWarningException(
        string patientId,
        string modality,
        IEnumerable<string> duplicateTarifIds,
        string message)
        : this(message, patientId, modality, duplicateTarifIds)
    {
    }
}
