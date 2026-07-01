using MediatR;

namespace Farinv.Application.BrgContext.KlasifikasiFeature.UseCases;

public record GolonganListQuery() : IRequest<IEnumerable<GolonganListResponse>>;

public record GolonganListResponse(string GolonganId, string GolonganName);

public class GolonganListHandler : IRequestHandler<GolonganListQuery, IEnumerable<GolonganListResponse>>
{
    private readonly IGolonganRepo _golonganRepo;

    public GolonganListHandler(IGolonganRepo golonganRepo)
    {
        _golonganRepo = golonganRepo;
    }


    public Task<IEnumerable<GolonganListResponse>> Handle(GolonganListQuery request, CancellationToken cancellationToken)
    {
        var listGolongan = _golonganRepo.ListData()?.ToList() ?? [];
        var response = listGolongan
            .OrderBy(x => x.GolonganName)
            .Select(x => new GolonganListResponse(x.GolonganId, x.GolonganName));

        return Task.FromResult(response);
    }
}