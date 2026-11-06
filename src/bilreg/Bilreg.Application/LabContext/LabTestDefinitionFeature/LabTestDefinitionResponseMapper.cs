using Bilreg.Domain.LabContext.LabTestDefinitionFeature;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

internal static class LabTestDefinitionResponseMapper
{
    public static LabTestDefinitionDetailResponse ToDetailResponse(LabTestDefinitionModel model, decimal tarifPrice = 0m) =>
        new(
            model.TestDefinitionId,
            model.TarifId,
            model.TarifCode,
            model.TarifName,
            model.LabTestCode,
            model.LabTestName,
            model.SpecimenType,
            (int)model.VacutainerType,
            model.IsActive,
            model.Components.Select(x => new LabTestComponentResponse(
                x.SequenceNo,
                x.ComponentId,
                x.ReferenceRangeOverride,
                x.RequiredFlagging,
                x.IsMandatory)).ToList(),
            tarifPrice);

    public static LabTestDefinitionListResponse ToListResponse(LabTestDefinitionModel model, decimal tarifPrice = 0m) =>
        new(
            model.TestDefinitionId,
            model.TarifId,
            model.TarifCode,
            model.TarifName,
            model.LabTestCode,
            model.LabTestName,
            model.SpecimenType,
            (int)model.VacutainerType,
            model.IsActive,
            model.Components.Count(),
            tarifPrice);
}

public record LabTestDefinitionListResponse(
    string TestDefinitionId,
    string TarifId,
    string TarifCode,
    string TarifName,
    string LabTestCode,
    string LabTestName,
    string SpecimenType,
    int VacutainerType,
    bool IsActive,
    int ComponentCount,
    decimal TarifPrice = 0m);

public record LabTestDefinitionDetailResponse(
    string TestDefinitionId,
    string TarifId,
    string TarifCode,
    string TarifName,
    string LabTestCode,
    string LabTestName,
    string SpecimenType,
    int VacutainerType,
    bool IsActive,
    IReadOnlyList<LabTestComponentResponse> Components,
    decimal TarifPrice = 0m);

public record LabTestComponentResponse(
    int SequenceNo,
    string ComponentId,
    string ReferenceRangeOverride,
    bool RequiredFlagging,
    bool IsMandatory);
