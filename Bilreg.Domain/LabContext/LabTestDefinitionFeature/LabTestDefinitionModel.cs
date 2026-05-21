using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionModel : ILabTestDefinitionKey
{
    private readonly List<LabTestComponentModel> _components;

    #region CREATION

    public LabTestDefinitionModel(
        string testDefinitionId,
        string tarifId,
        string tarifCode,
        string tarifName,
        string labTestCode,
        string labTestName,
        string specimenType,
        VacutainerTypeEnum vacutainerType,
        bool isActive,
        AuditTrailType auditTrail,
        IEnumerable<LabTestComponentModel> components)
    {
        Guard.Against.NullOrWhiteSpace(testDefinitionId, nameof(testDefinitionId));
        Guard.Against.NullOrWhiteSpace(tarifId, nameof(tarifId));
        Guard.Against.NullOrWhiteSpace(labTestCode, nameof(labTestCode));
        Guard.Against.NullOrWhiteSpace(labTestName, nameof(labTestName));
        Guard.Against.Null(auditTrail);

        if (!LabMasterIdFormat.IsValidLtd(testDefinitionId))
            throw new ArgumentException(
                "LAB_INVALID_LTD_FORMAT: TestDefinitionId must be LTD + 4 uppercase hex.",
                nameof(testDefinitionId));

        if (!Enum.IsDefined(typeof(VacutainerTypeEnum), vacutainerType))
            throw new ArgumentException("VacutainerType is not valid.", nameof(vacutainerType));

        TestDefinitionId = testDefinitionId;
        TarifId = tarifId;
        TarifCode = tarifCode ?? string.Empty;
        TarifName = tarifName ?? string.Empty;
        LabTestCode = labTestCode;
        LabTestName = labTestName;
        SpecimenType = specimenType ?? string.Empty;
        VacutainerType = vacutainerType;
        IsActive = isActive;
        AuditTrail = auditTrail;
        _components = components?.ToList() ?? [];
    }

    public static LabTestDefinitionModel Default =>
        new("-", "-", "-", "-", "-", "-", "-", VacutainerTypeEnum.Edta, false,
            AuditTrailType.Default, []);

    public static ILabTestDefinitionKey Key(string testDefinitionId) =>
        Default with { TestDefinitionId = testDefinitionId };

    public static LabTestDefinitionModel CreateNew(
        string testDefinitionId,
        string tarifId,
        string tarifCode,
        string tarifName,
        string labTestCode,
        string labTestName,
        string specimenType,
        VacutainerTypeEnum vacutainerType,
        bool isActive,
        string userId,
        IEnumerable<LabTestComponentModel> components,
        IReadOnlyDictionary<string, LabComponentMasterModel> componentCatalog)
    {
        var model = new LabTestDefinitionModel(
            testDefinitionId,
            tarifId,
            tarifCode,
            tarifName,
            labTestCode,
            labTestName,
            specimenType,
            vacutainerType,
            isActive,
            AuditTrailType.Create(userId, DateTime.Now),
            components);

        model.ValidateComponentStructure(componentCatalog);
        if (isActive)
            model.ValidateActiveInvariants(componentCatalog);

        return model;
    }

    #endregion

    public string TestDefinitionId { get; init; }
    public string TarifId { get; init; }
    public string TarifCode { get; init; }
    public string TarifName { get; init; }
    public string LabTestCode { get; init; }
    public string LabTestName { get; init; }
    public string SpecimenType { get; init; }
    public VacutainerTypeEnum VacutainerType { get; init; }
    public bool IsActive { get; init; }
    public AuditTrailType AuditTrail { get; init; }

    public IEnumerable<LabTestComponentModel> Components => _components;

    public LabTestDefinitionModel ApplyUpdate(
        string tarifId,
        string tarifCode,
        string tarifName,
        string labTestCode,
        string labTestName,
        string specimenType,
        VacutainerTypeEnum vacutainerType,
        bool isActive,
        IEnumerable<LabTestComponentModel> components,
        string userId,
        IReadOnlyDictionary<string, LabComponentMasterModel> componentCatalog)
    {
        var model = new LabTestDefinitionModel(
            TestDefinitionId,
            tarifId,
            tarifCode,
            tarifName,
            labTestCode,
            labTestName,
            specimenType,
            vacutainerType,
            isActive,
            ModifiedAudit(AuditTrail, userId),
            components);

        model.ValidateComponentStructure(componentCatalog);
        if (isActive)
            model.ValidateActiveInvariants(componentCatalog);

        return model;
    }

    public LabTestDefinitionModel Activate(
        string userId,
        IReadOnlyDictionary<string, LabComponentMasterModel> componentCatalog)
    {
        if (IsActive)
            return this;

        var model = new LabTestDefinitionModel(
            TestDefinitionId,
            TarifId,
            TarifCode,
            TarifName,
            LabTestCode,
            LabTestName,
            SpecimenType,
            VacutainerType,
            true,
            ModifiedAudit(AuditTrail, userId),
            _components);

        model.ValidateActiveInvariants(componentCatalog);
        return model;
    }

    public LabTestDefinitionModel Deactivate(string userId)
    {
        if (!IsActive)
            return this;

        return new LabTestDefinitionModel(
            TestDefinitionId,
            TarifId,
            TarifCode,
            TarifName,
            LabTestCode,
            LabTestName,
            SpecimenType,
            VacutainerType,
            false,
            ModifiedAudit(AuditTrail, userId),
            _components);
    }

    private static AuditTrailType ModifiedAudit(AuditTrailType current, string userId)
    {
        var audit = new AuditTrailType(current.Created, current.Modified, current.Voided);
        audit.Modif(userId, DateTime.Now);
        return audit;
    }

    private void ValidateComponentStructure(IReadOnlyDictionary<string, LabComponentMasterModel> componentCatalog)
    {
        if (_components.Count == 0)
            return;

        if (_components.GroupBy(x => x.SequenceNo).Any(g => g.Count() > 1))
            throw new InvalidOperationException(
                "LAB_DUPLICATE_SEQUENCE: SequenceNo must be unique within a LabTestDefinition.");

        if (_components.GroupBy(x => x.ComponentId).Any(g => g.Count() > 1))
            throw new InvalidOperationException(
                "LAB_DUPLICATE_COMPONENT: ComponentId must be unique within a LabTestDefinition.");

        foreach (var line in _components)
        {
            if (!componentCatalog.TryGetValue(line.ComponentId, out var master))
                throw new InvalidOperationException(
                    $"LAB_COMPONENT_NOT_FOUND: LabComponentMaster '{line.ComponentId}' not found.");

            if (!master.IsActive)
                throw new InvalidOperationException(
                    $"LAB_COMPONENT_INACTIVE: LabComponentMaster '{line.ComponentId}' is inactive.");
        }
    }

    private void ValidateActiveInvariants(IReadOnlyDictionary<string, LabComponentMasterModel> componentCatalog)
    {
        if (_components.Count == 0)
            throw new InvalidOperationException(
                "LAB_TEST_DEFINITION_EMPTY_COMPONENTS: Active LabTestDefinition must contain at least one LabTestComponent.");

        ValidateComponentStructure(componentCatalog);
    }
}

public interface ILabTestDefinitionKey
{
    string TestDefinitionId { get; }
}
