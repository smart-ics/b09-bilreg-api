using Bilreg.Domain.LabContext.LabTestDefinitionFeature;

namespace Bilreg.Infrastructure.LabContext.LabTestDefinitionFeature;

public record LabTestComponentDto(
    string TestDefinitionId,
    int SequenceNo,
    string ComponentId,
    string ReferenceRangeOverride,
    bool RequiredFlagging,
    bool IsMandatory)
{
    public static LabTestComponentDto FromModel(string testDefinitionId, LabTestComponentModel model) =>
        new(
            testDefinitionId,
            model.SequenceNo,
            model.ComponentId,
            model.ReferenceRangeOverride,
            model.RequiredFlagging,
            model.IsMandatory);

    public LabTestComponentModel ToModel() =>
        new(
            SequenceNo,
            ComponentId,
            ReferenceRangeOverride,
            RequiredFlagging,
            IsMandatory);
}
