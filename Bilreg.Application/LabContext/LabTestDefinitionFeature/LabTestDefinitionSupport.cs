using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

internal static class LabTestDefinitionSupport
{
    public static IReadOnlyDictionary<string, LabComponentMasterModel> BuildComponentCatalog(
        ILabComponentMasterRepo componentMasterRepo,
        IEnumerable<LabTestComponentInput> inputs)
    {
        var ids = inputs.Select(x => x.ComponentId).Distinct().ToList();
        var catalog = new Dictionary<string, LabComponentMasterModel>(StringComparer.Ordinal);

        foreach (var componentId in ids)
        {
            componentMasterRepo.LoadEntity(LabComponentMasterModel.Key(componentId))
                .Match(
                    onSome: m => catalog[componentId] = m,
                    onNone: () => { });
        }

        return catalog;
    }

    public static IEnumerable<LabTestComponentModel> ToComponentModels(IEnumerable<LabTestComponentInput> inputs) =>
        inputs.Select(x => new LabTestComponentModel(
            x.SequenceNo,
            x.ComponentId,
            x.ReferenceRangeOverride ?? string.Empty,
            x.RequiredFlagging,
            x.IsMandatory));

    public static void EnsureNoActiveTarifConflict(
        ILabTestDefinitionRepo repo,
        string tarifId,
        bool isActive,
        string? excludeTestDefinitionId)
    {
        if (!isActive)
            return;

        if (repo.HasActiveTarifConflict(tarifId, excludeTestDefinitionId))
            throw new InvalidOperationException(
                "LAB_TEST_DEFINITION_TARIF_CONFLICT: Another active LabTestDefinition already exists for this TarifId.");
    }
}
