using MediatR;

namespace Farinv.Application.BrgContext.KlasifikasiFeature.UseCases;

public record BentukListQuery() : IRequest<IEnumerable<BentukListResponse>>;

public record BentukListResponse(string BentukId, string BentukName);

public class BentukListHandler : IRequestHandler<BentukListQuery, IEnumerable<BentukListResponse>>
{
    private readonly IBentukRepo _bentukRepo;

    public BentukListHandler(IBentukRepo bentukRepo)
    {
        _bentukRepo = bentukRepo;
    }

    public Task<IEnumerable<BentukListResponse>> Handle(BentukListQuery request, CancellationToken cancellationToken)
    {
        var listBentuk = _bentukRepo.ListData()?.ToList() ?? [];
        var response = listBentuk
            .OrderBy(x => x.BentukName)
            .Select(x => new BentukListResponse(x.BentukId, x.BentukName));

        return Task.FromResult(response);
    }
}