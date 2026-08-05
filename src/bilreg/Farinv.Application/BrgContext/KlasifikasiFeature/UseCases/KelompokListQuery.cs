using MediatR;

namespace Farinv.Application.BrgContext.KlasifikasiFeature.UseCases;

public record KelompokListQuery() : IRequest<IEnumerable<KelompokListResponse>>;

public record KelompokListResponse(string KelompokId, string KelompokName);

public class KelompokListHandler : IRequestHandler<KelompokListQuery, IEnumerable<KelompokListResponse>>
{
    private readonly IKelompokRepo _kelompokRepo;

    public KelompokListHandler(IKelompokRepo kelompokRepo)
    {
        _kelompokRepo = kelompokRepo;
    }

    public Task<IEnumerable<KelompokListResponse>> Handle(KelompokListQuery request, CancellationToken cancellationToken)
    {
        var listKelompok = _kelompokRepo.ListData()?.ToList() ?? [];
        var response = listKelompok
            .OrderBy(x => x.KelompokName)
            .Select(x => new KelompokListResponse(x.KelompokId, x.KelompokName));

        return Task.FromResult(response);
    }
}