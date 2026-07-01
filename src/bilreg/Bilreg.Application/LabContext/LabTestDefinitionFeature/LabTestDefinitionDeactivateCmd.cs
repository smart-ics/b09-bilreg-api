using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionDeactivateCmd(string TestDefinitionId, string UserId)
    : IRequest, ILabTestDefinitionKey;

public class LabTestDefinitionDeactivateHandler : IRequestHandler<LabTestDefinitionDeactivateCmd>
{
    private readonly ILabTestDefinitionRepo _definitionRepo;

    public LabTestDefinitionDeactivateHandler(ILabTestDefinitionRepo definitionRepo)
    {
        _definitionRepo = definitionRepo;
    }

    public Task Handle(LabTestDefinitionDeactivateCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TestDefinitionId, nameof(request.TestDefinitionId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var existing = _definitionRepo.LoadEntity(request)
            .GetValueOrThrow($"LabTestDefinition '{request.TestDefinitionId}' not found");

        var deactivated = existing.Deactivate(request.UserId);
        _definitionRepo.SaveChanges(deactivated);
        return Task.CompletedTask;
    }
}
