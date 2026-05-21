namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionListFilter(
    bool ActiveOnly = false,
    string? Search = null,
    string? TarifId = null);
