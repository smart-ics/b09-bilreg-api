namespace Bilreg.Domain.LabContext.LabResultFeature;

/// <summary>
/// Server-owned result line structure derived from <c>LabOrderItemComponent</c> snapshots.
/// </summary>
public record LabResultScaffoldLine(
    string ComponentId,
    string ComponentCode,
    string ComponentName,
    LabResultTypeEnum ResultType,
    string Unit,
    string ReferenceRangeText,
    int SequenceNo,
    bool IsMandatory,
    string TestDefinitionId,
    string LabTestName);
