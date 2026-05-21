using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public class LabTestResolutionService : ILabTestResolutionService
{
    private const int DefaultRequiredTubeCount = 1;

    private readonly ILabTestDefinitionRepo _definitionRepo;
    private readonly ILabComponentMasterRepo _componentMasterRepo;

    public LabTestResolutionService(
        ILabTestDefinitionRepo definitionRepo,
        ILabComponentMasterRepo componentMasterRepo)
    {
        _definitionRepo = definitionRepo;
        _componentMasterRepo = componentMasterRepo;
    }

    public IReadOnlyList<ResolvedLabOrderLine> ResolveByTarifItems(IEnumerable<LabOrderTarifItemInput> items)
    {
        var inputList = items?.ToList() ?? [];
        if (inputList.Count == 0)
            throw new ArgumentException("At least one Tarif line is required.", nameof(items));

        var lines = new List<ResolvedLabOrderLine>();
        foreach (var input in inputList)
        {
            if (string.IsNullOrWhiteSpace(input.TarifId))
                throw new ArgumentException("TarifId is required on each order line.", nameof(items));

            var definition = ResolveDefinition(input.TarifId);
            var catalog = BuildComponentCatalog(definition.Components);
            definition.ValidateForOrderResolution(catalog);

            var tarifName = string.IsNullOrWhiteSpace(input.TarifName)
                ? definition.TarifName
                : input.TarifName;

            var item = LabOrderItemModel.Create(
                definition.TestDefinitionId,
                definition.LabTestCode,
                definition.LabTestName,
                definition.TarifId,
                definition.TarifCode,
                tarifName,
                definition.VacutainerType,
                definition.SpecimenType,
                DefaultRequiredTubeCount);

            var components = definition.Components
                .OrderBy(x => x.SequenceNo)
                .Select(line =>
                {
                    var master = catalog[line.ComponentId];
                    return new LabOrderItemComponentModel(
                        ItemNo: 0,
                        ComponentNo: 0,
                        ComponentId: master.ComponentId,
                        ComponentCode: master.ComponentCode,
                        ComponentName: master.ComponentName,
                        ResultType: (int)master.ResultType,
                        Unit: master.DefaultUnit,
                        ReferenceRangeText: line.ReferenceRangeOverride,
                        SequenceNo: line.SequenceNo,
                        IsMandatory: line.IsMandatory);
                })
                .ToList();

            lines.Add(new ResolvedLabOrderLine(item, components));
        }

        return lines;
    }

    private LabTestDefinitionModel ResolveDefinition(string tarifId)
    {
        var active = _definitionRepo.LoadActiveByTarifId(tarifId);
        if (active.HasValue)
            return active.Value;

        var any = _definitionRepo
            .ListData(new LabTestDefinitionListFilter(TarifId: tarifId))
            .FirstOrDefault();

        if (any is not null && !any.IsActive)
            throw new InvalidOperationException(
                $"LAB_TEST_DEFINITION_INACTIVE: LabTestDefinition '{any.TestDefinitionId}' is inactive.");

        throw new InvalidOperationException(
            $"LAB_TEST_DEFINITION_NOT_FOUND: No active LabTestDefinition for TarifId '{tarifId}'.");
    }

    private IReadOnlyDictionary<string, LabComponentMasterModel> BuildComponentCatalog(
        IEnumerable<LabTestComponentModel> components)
    {
        var catalog = new Dictionary<string, LabComponentMasterModel>(StringComparer.Ordinal);
        foreach (var line in components)
        {
            if (catalog.ContainsKey(line.ComponentId))
                continue;

            _componentMasterRepo.LoadEntity(LabComponentMasterModel.Key(line.ComponentId))
                .Match(
                    onSome: m =>
                    {
                        if (!m.IsActive)
                            throw new InvalidOperationException(
                                $"LAB_COMPONENT_INACTIVE: LabComponentMaster '{line.ComponentId}' is inactive.");
                        catalog[line.ComponentId] = m;
                    },
                    onNone: () => throw new InvalidOperationException(
                        $"LAB_COMPONENT_NOT_FOUND: LabComponentMaster '{line.ComponentId}' not found."));
        }

        return catalog;
    }
}
