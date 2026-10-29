namespace Bilreg.Application.LabContext.LabOrderFeature;

public class DuplicateLabOrderWarningException : InvalidOperationException
{
    public IReadOnlyList<string> DuplicateTestNames { get; }
    public IReadOnlyList<string> PreviousOrderIds { get; }

    public DuplicateLabOrderWarningException(
        string message,
        IEnumerable<string>? duplicateTestNames = null,
        IEnumerable<string>? previousOrderIds = null)
        : base(message)
    {
        DuplicateTestNames = duplicateTestNames?.ToList() ?? [];
        PreviousOrderIds = previousOrderIds?.ToList() ?? [];
    }

    public DuplicateLabOrderWarningException(
        IEnumerable<string> duplicateTestNames,
        IEnumerable<string> previousOrderIds,
        string message)
        : this(message, duplicateTestNames, previousOrderIds)
    {
    }
}
