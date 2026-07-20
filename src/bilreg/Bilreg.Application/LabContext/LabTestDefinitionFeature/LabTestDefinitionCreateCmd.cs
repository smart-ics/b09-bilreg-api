using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionCreateCmd(
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
    : IRequest<LabTestDefinitionCreateResponse>;

public record LabTestDefinitionCreateResponse(string TestDefinitionId);

public class LabTestDefinitionCreateHandler
    : IRequestHandler<LabTestDefinitionCreateCmd, LabTestDefinitionCreateResponse>
{
    private readonly ILabTestDefinitionRepo _definitionRepo;
    private readonly ILabComponentMasterRepo _componentMasterRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public LabTestDefinitionCreateHandler(
        ILabTestDefinitionRepo definitionRepo,
        ILabComponentMasterRepo componentMasterRepo,
        ITglJamProvider? tglJamProvider = null)
    {
        _definitionRepo = definitionRepo;
        _componentMasterRepo = componentMasterRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<LabTestDefinitionCreateResponse> Handle(
        LabTestDefinitionCreateCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TarifId, nameof(request.TarifId));
        Guard.Against.NullOrWhiteSpace(request.LabTestCode, nameof(request.LabTestCode));
        Guard.Against.NullOrWhiteSpace(request.LabTestName, nameof(request.LabTestName));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.Null(request.Components);

        LabTestDefinitionSupport.EnsureNoActiveTarifConflict(
            _definitionRepo, request.TarifId, request.IsActive, null);

        var catalog = LabTestDefinitionSupport.BuildComponentCatalog(
            _componentMasterRepo, request.Components);
        var components = LabTestDefinitionSupport.ToComponentModels(request.Components);
        var testDefinitionId = _definitionRepo.AllocateNextTestDefinitionId();

        var model = LabTestDefinitionModel.CreateNew(
            testDefinitionId,
            request.TarifId,
            request.TarifCode,
            request.TarifName,
            request.LabTestCode,
            request.LabTestName,
            request.SpecimenType,
            request.VacutainerType,
            request.IsActive,
            request.UserId,
            _tglJamProvider.Now,
            components,
            catalog);

        _definitionRepo.SaveChanges(model);
        return Task.FromResult(new LabTestDefinitionCreateResponse(testDefinitionId));
    }
}
