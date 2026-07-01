namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

public record LabTestComponentInput(
    int SequenceNo,
    string ComponentId,
    string? ReferenceRangeOverride,
    bool RequiredFlagging,
    bool IsMandatory);
