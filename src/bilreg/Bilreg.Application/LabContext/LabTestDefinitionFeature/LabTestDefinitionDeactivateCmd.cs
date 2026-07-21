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
    private readonly ITglJamProvider _tglJamProvider;

    public LabTestDefinitionDeactivateHandler(ILabTestDefinitionRepo definitionRepo,
        ITglJamProvider tglJamProvider)
    {
        _definitionRepo = definitionRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(LabTestDefinitionDeactivateCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TestDefinitionId, nameof(request.TestDefinitionId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var existing = _definitionRepo.LoadEntity(request)
            .GetValueOrThrow($"LabTestDefinition '{request.TestDefinitionId}' not found");

        var occurredAt = _tglJamProvider.Now;
        var deactivated = existing.Deactivate(request.UserId, occurredAt);
        _definitionRepo.SaveChanges(deactivated);
        return Task.CompletedTask;
    }
}
