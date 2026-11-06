using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public class LabTestResolutionService : ILabTestResolutionService
{
    private const int DefaultRequiredTubeCount = 1;

    private readonly ILabTestDefinitionRepo _definitionRepo;
    private readonly ILabComponentMasterRepo _componentMasterRepo;
    private readonly ILabDisciplineRepo? _disciplineRepo;
    private readonly ILabTestGroupRepo? _testGroupRepo;

    public LabTestResolutionService(
        ILabTestDefinitionRepo definitionRepo,
        ILabComponentMasterRepo componentMasterRepo,
        ILabDisciplineRepo? disciplineRepo = null,
        ILabTestGroupRepo? testGroupRepo = null)
    {
        _definitionRepo = definitionRepo;
        _componentMasterRepo = componentMasterRepo;
        _disciplineRepo = disciplineRepo;
        _testGroupRepo = testGroupRepo;
    }

    public IReadOnlyList<ResolvedLabOrderLine> ResolveByTarifItems(IEnumerable<LabOrderTarifItemInput> items)
    {
        var inputList = items?.ToList() ?? [];
        if (inputList.Count == 0)
            throw new ArgumentException("At least one Tarif line is required.", nameof(items));

        var lines = new List<ResolvedLabOrderLine>();
        var disciplineCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var testGroupCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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

            var requiredTubeCount = definition.VacutainerType == VacutainerTypeEnum.None
                ? 0
                : DefaultRequiredTubeCount;

            var disciplineName = ResolveDisciplineName(definition.DisciplineId, disciplineCache);
            var testGroupName = ResolveTestGroupName(definition.TestGroupId, testGroupCache);

            var item = LabOrderItemModel.Create(
                definition.TestDefinitionId,
                definition.LabTestCode,
                definition.LabTestName,
                definition.TarifId,
                definition.TarifCode,
                tarifName,
                definition.VacutainerType,
                definition.SpecimenType,
                requiredTubeCount,
                (LabOrderUrgencyEnum)input.Urgency,
                definition.ServiceType,
                definition.DisciplineId,
                disciplineName,
                definition.TestGroupId,
                testGroupName);

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

    private string ResolveDisciplineName(string disciplineId, IDictionary<string, string> cache)
    {
        if (string.IsNullOrWhiteSpace(disciplineId))
            return string.Empty;

        if (cache.TryGetValue(disciplineId, out var name))
            return name;

        name = string.Empty;
        if (_disciplineRepo is not null)
        {
            var entity = _disciplineRepo.LoadEntity(LabDisciplineModel.Key(disciplineId));
            if (entity.HasValue)
                name = entity.Value.DisciplineName;
        }

        cache[disciplineId] = name;
        return name;
    }

    private string ResolveTestGroupName(string testGroupId, IDictionary<string, string> cache)
    {
        if (string.IsNullOrWhiteSpace(testGroupId))
            return string.Empty;

        if (cache.TryGetValue(testGroupId, out var name))
            return name;

        name = string.Empty;
        if (_testGroupRepo is not null)
        {
            var entity = _testGroupRepo.LoadEntity(LabTestGroupModel.Key(testGroupId));
            if (entity.HasValue)
                name = entity.Value.TestGroupName;
        }

        cache[testGroupId] = name;
        return name;
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
