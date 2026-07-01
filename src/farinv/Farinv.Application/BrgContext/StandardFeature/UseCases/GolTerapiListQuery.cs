using MediatR;

namespace Farinv.Application.BrgContext.StandardFeature.UseCases;

public record GolTerapiListQuery() : IRequest<IEnumerable<GolTerapiListResponse>>;

public record GolTerapiListResponse(string GolTerapiId, string GolTerapiName);

public class GolTerapiListHandler 
    : IRequestHandler<GolTerapiListQuery, IEnumerable<GolTerapiListResponse>>
{
    private readonly IGolTerapiRepo _golTerapiRepo;

    public GolTerapiListHandler(IGolTerapiRepo golTerapiRepo)
    {
        _golTerapiRepo = golTerapiRepo;
    }

    public Task<IEnumerable<GolTerapiListResponse>> Handle(GolTerapiListQuery request, 
        CancellationToken cancellationToken)
    {
        var listGolTerapi = _golTerapiRepo.ListData()?.ToList() ?? [];
        var response = listGolTerapi
            .OrderBy(x => x.GolTerapiName)
            .Select(x => new GolTerapiListResponse(x.GolTerapiId, x.GolTerapiName));

        return Task.FromResult(response);
    }
}