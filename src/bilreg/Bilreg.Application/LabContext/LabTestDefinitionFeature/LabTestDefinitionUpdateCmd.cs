using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionUpdateCmd(
    string TestDefinitionId,
    string TarifId,
    string TarifCode,
    string TarifName,
    string LabTestCode,
    string LabTestName,
    string SpecimenType,
    VacutainerTypeEnum VacutainerType,
    bool IsActive,
    string UserId,
    IReadOnlyList<LabTestComponentInput> Components)
    : IRequest, ILabTestDefinitionKey;

public class LabTestDefinitionUpdateHandler : IRequestHandler<LabTestDefinitionUpdateCmd>
{
    private readonly ILabTestDefinitionRepo _definitionRepo;
    private readonly ILabComponentMasterRepo _componentMasterRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public LabTestDefinitionUpdateHandler(
        ILabTestDefinitionRepo definitionRepo,
        ILabComponentMasterRepo componentMasterRepo,
        ITglJamProvider? tglJamProvider = null)
    {
        _definitionRepo = definitionRepo;
        _componentMasterRepo = componentMasterRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(LabTestDefinitionUpdateCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TestDefinitionId, nameof(request.TestDefinitionId));
        Guard.Against.NullOrWhiteSpace(request.TarifId, nameof(request.TarifId));
        Guard.Against.NullOrWhiteSpace(request.LabTestCode, nameof(request.LabTestCode));
        Guard.Against.NullOrWhiteSpace(request.LabTestName, nameof(request.LabTestName));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.Null(request.Components);

        if (!LabMasterIdFormat.IsValidLtd(request.TestDefinitionId))
            throw new ArgumentException(
                "LAB_INVALID_LTD_FORMAT: TestDefinitionId must be LTD + 4 uppercase hex.",
                nameof(request.TestDefinitionId));

        var existing = _definitionRepo.LoadEntity(request)
            .GetValueOrThrow($"LabTestDefinition '{request.TestDefinitionId}' not found");

        LabTestDefinitionSupport.EnsureNoActiveTarifConflict(
            _definitionRepo, request.TarifId, request.IsActive, request.TestDefinitionId);

        var catalog = LabTestDefinitionSupport.BuildComponentCatalog(
            _componentMasterRepo, request.Components);
        var components = LabTestDefinitionSupport.ToComponentModels(request.Components);

        var updated = existing.ApplyUpdate(
            request.TarifId,
            request.TarifCode,
            request.TarifName,
            request.LabTestCode,
            request.LabTestName,
            request.SpecimenType,
            request.VacutainerType,
            request.IsActive,
            components,
            request.UserId,
            _tglJamProvider.Now,
            catalog);

        _definitionRepo.SaveChanges(updated);
        return Task.CompletedTask;
    }
}
