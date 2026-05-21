namespace Bilreg.Domain.LabContext.LabOrderFeature;

/// <summary>
/// Immutable order-line component snapshot. Persistence-only child — not an aggregate.
/// </summary>
public record LabOrderItemComponentModel(
    int ItemNo,
    int ComponentNo,
    string ComponentId,
    string ComponentCode,
    string ComponentName,
    int ResultType,
    string Unit,
    string ReferenceRangeText,
    int SequenceNo,
    bool IsMandatory);
