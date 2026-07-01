using MediatR;

namespace Bilreg.Application.LabContext.LabComponentMasterFeature;

public record LabComponentMasterListQuery(bool ActiveOnly = true, string? Search = null)
    : IRequest<IEnumerable<LabComponentMasterListResponse>>;

public record LabComponentMasterListResponse(
    string ComponentId,
    string? LoincCode,
    string ComponentCode,
    string ComponentName,
    string? ComponentNameIndonesia,
    int ResultType,
    string DefaultUnit,
    bool IsSystem,
    bool IsActive);

public class LabComponentMasterListHandler
    : IRequestHandler<LabComponentMasterListQuery, IEnumerable<LabComponentMasterListResponse>>
{
    private readonly ILabComponentMasterRepo _repo;

    public LabComponentMasterListHandler(ILabComponentMasterRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<LabComponentMasterListResponse>> Handle(
        LabComponentMasterListQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new LabComponentMasterListFilter(request.ActiveOnly, request.Search);
        var list = _repo.ListData(filter).ToList();
        var response = list.Select(x => new LabComponentMasterListResponse(
            x.ComponentId,
            x.LoincCode,
            x.ComponentCode,
            x.ComponentName,
            x.ComponentNameIndonesia,
            (int)x.ResultType,
            x.DefaultUnit,
            x.IsSystem,
            x.IsActive));

        return Task.FromResult(response);
    }
}
