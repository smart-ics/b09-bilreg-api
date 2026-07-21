using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionActivateCmd(string TestDefinitionId, string UserId)
    : IRequest, ILabTestDefinitionKey;

public class LabTestDefinitionActivateHandler : IRequestHandler<LabTestDefinitionActivateCmd>
{
    private readonly ILabTestDefinitionRepo _definitionRepo;
    private readonly ILabComponentMasterRepo _componentMasterRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public LabTestDefinitionActivateHandler(
        ILabTestDefinitionRepo definitionRepo,
        ILabComponentMasterRepo componentMasterRepo,
        ITglJamProvider tglJamProvider)
    {
        _definitionRepo = definitionRepo;
        _componentMasterRepo = componentMasterRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(LabTestDefinitionActivateCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TestDefinitionId, nameof(request.TestDefinitionId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var existing = _definitionRepo.LoadEntity(request)
            .GetValueOrThrow($"LabTestDefinition '{request.TestDefinitionId}' not found");

        LabTestDefinitionSupport.EnsureNoActiveTarifConflict(
            _definitionRepo, existing.TarifId, true, request.TestDefinitionId);

        var catalog = BuildCatalogFromExistingComponents(existing);
        var occurredAt = _tglJamProvider.Now;
        var activated = existing.Activate(request.UserId, occurredAt, catalog);
        _definitionRepo.SaveChanges(activated);
        return Task.CompletedTask;
    }

    private IReadOnlyDictionary<string, LabComponentMasterModel> BuildCatalogFromExistingComponents(
        LabTestDefinitionModel existing)
    {
        var inputs = existing.Components.Select(x => new LabTestComponentInput(
            x.SequenceNo,
            x.ComponentId,
            x.ReferenceRangeOverride,
            x.RequiredFlagging,
            x.IsMandatory));
        return LabTestDefinitionSupport.BuildComponentCatalog(_componentMasterRepo, inputs);
    }
}
