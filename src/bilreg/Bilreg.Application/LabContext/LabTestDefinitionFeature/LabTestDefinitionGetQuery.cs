using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionGetQuery(string TestDefinitionId)
    : IRequest<LabTestDefinitionDetailResponse>, ILabTestDefinitionKey;

public class LabTestDefinitionGetHandler
    : IRequestHandler<LabTestDefinitionGetQuery, LabTestDefinitionDetailResponse>
{
    private readonly ILabTestDefinitionRepo _repo;

    public LabTestDefinitionGetHandler(ILabTestDefinitionRepo repo)
    {
        _repo = repo;
    }

    public Task<LabTestDefinitionDetailResponse> Handle(
        LabTestDefinitionGetQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TestDefinitionId, nameof(request.TestDefinitionId));

        var model = _repo.LoadEntity(request)
            .GetValueOrThrow($"LabTestDefinition '{request.TestDefinitionId}' not found");

        return Task.FromResult(LabTestDefinitionResponseMapper.ToDetailResponse(model));
    }
}
