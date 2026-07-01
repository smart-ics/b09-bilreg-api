using MediatR;

namespace Farinv.Application.BrgContext.StandardFeature.UseCases;

public record KelasTerapiListQuery() : IRequest<IEnumerable<KelasTerapiListResponse>>;

public record KelasTerapiListResponse(string KelasTerapiId, string KelasTerapiName);

public class KelasTerapiListHandler
    : IRequestHandler<KelasTerapiListQuery, IEnumerable<KelasTerapiListResponse>>
{
    private readonly IKelasTerapiRepo _kelasTerapiRepo;

    public KelasTerapiListHandler(IKelasTerapiRepo kelasTerapiRepo)
    {
        _kelasTerapiRepo = kelasTerapiRepo;
    }

    public Task<IEnumerable<KelasTerapiListResponse>> Handle(KelasTerapiListQuery request, 
        CancellationToken cancellationToken)
    {
        var listKelasTerapi = _kelasTerapiRepo.ListData()?.ToList() ?? [];
        var response = listKelasTerapi
            .OrderBy(x => x.KelasTerapiName)
            .Select(x => new KelasTerapiListResponse(x.KelasTerapiId, x.KelasTerapiName));

        return Task.FromResult(response);
    }
}