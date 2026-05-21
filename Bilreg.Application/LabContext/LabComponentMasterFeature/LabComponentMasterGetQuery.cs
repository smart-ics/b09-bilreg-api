using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabComponentMasterFeature;

public record LabComponentMasterGetQuery(string ComponentId)
    : IRequest<LabComponentMasterGetResponse>, ILabComponentMasterKey;

public record LabComponentMasterGetResponse(
    string ComponentId,
    string? LoincCode,
    string ComponentCode,
    string ComponentName,
    string? ComponentNameIndonesia,
    int ResultType,
    string DefaultUnit,
    bool IsSystem,
    bool IsActive);

public class LabComponentMasterGetHandler
    : IRequestHandler<LabComponentMasterGetQuery, LabComponentMasterGetResponse>
{
    private readonly ILabComponentMasterRepo _repo;

    public LabComponentMasterGetHandler(ILabComponentMasterRepo repo)
    {
        _repo = repo;
    }

    public Task<LabComponentMasterGetResponse> Handle(
        LabComponentMasterGetQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ComponentId, nameof(request.ComponentId));

        var component = _repo.LoadEntity(request)
            .GetValueOrThrow($"LabComponentMaster '{request.ComponentId}' not found");

        var response = new LabComponentMasterGetResponse(
            component.ComponentId,
            component.LoincCode,
            component.ComponentCode,
            component.ComponentName,
            component.ComponentNameIndonesia,
            (int)component.ResultType,
            component.DefaultUnit,
            component.IsSystem,
            component.IsActive);

        return Task.FromResult(response);
    }
}
