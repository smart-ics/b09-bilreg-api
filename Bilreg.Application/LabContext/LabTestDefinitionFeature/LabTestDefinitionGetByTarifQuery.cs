using Ardalis.GuardClauses;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionGetByTarifQuery(string TarifId)
    : IRequest<LabTestDefinitionDetailResponse>;

public class LabTestDefinitionGetByTarifHandler
    : IRequestHandler<LabTestDefinitionGetByTarifQuery, LabTestDefinitionDetailResponse>
{
    private readonly ILabTestDefinitionRepo _repo;

    public LabTestDefinitionGetByTarifHandler(ILabTestDefinitionRepo repo)
    {
        _repo = repo;
    }

    public Task<LabTestDefinitionDetailResponse> Handle(
        LabTestDefinitionGetByTarifQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TarifId, nameof(request.TarifId));

        var model = _repo.LoadActiveByTarifId(request.TarifId)
            .GetValueOrThrow($"LAB_TEST_DEFINITION_NOT_FOUND: No active LabTestDefinition for TarifId '{request.TarifId}'.");

        if (!model.IsActive)
            throw new InvalidOperationException(
                $"LAB_TEST_DEFINITION_INACTIVE: LabTestDefinition '{model.TestDefinitionId}' is inactive.");

        return Task.FromResult(LabTestDefinitionResponseMapper.ToDetailResponse(model));
    }
}
