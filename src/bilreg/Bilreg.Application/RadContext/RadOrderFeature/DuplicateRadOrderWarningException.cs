namespace Bilreg.Application.RadContext.RadOrderFeature;

public class DuplicateRadOrderWarningException : InvalidOperationException
{
    public const string Code = "RAD_DUPLICATE_ORDER_WARNING";
    public string WarningCode => Code;

    public string PatientId { get; }
    public string Modality { get; }
    public IReadOnlyList<string> DuplicateTarifIds { get; }
    public IReadOnlyList<string> ExistingOrderNos { get; }

    public DuplicateRadOrderWarningException(
        string patientId,
        string modality,
        IEnumerable<string> duplicateTarifIds,
        string message,
        IEnumerable<string>? existingOrderNos = null)
        : base(message)
    {
        PatientId = patientId ?? string.Empty;
        Modality = modality ?? string.Empty;
        DuplicateTarifIds = duplicateTarifIds?.ToList() ?? [];
        ExistingOrderNos = existingOrderNos?.ToList() ?? [];
    }

    public DuplicateRadOrderWarningException(string message)
        : this(string.Empty, string.Empty, [], message, null)
    {
    }
}
