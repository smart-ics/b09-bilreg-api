using MediatR;

namespace Farinv.Application.BrgContext.KlasifikasiFeature.UseCases;

public record PabrikListQuery() : IRequest<IEnumerable<PabrikListResponse>>;

public record PabrikListResponse(string PabrikId, string PabrikName);

public class PabrikListHandler : IRequestHandler<PabrikListQuery, IEnumerable<PabrikListResponse>>
{
    private readonly IPabrikRepo _pabrikRepo;

    public PabrikListHandler(IPabrikRepo pabrikRepo)
    {
        _pabrikRepo = pabrikRepo;
    }

    public Task<IEnumerable<PabrikListResponse>> Handle(PabrikListQuery request, 
        CancellationToken cancellationToken)
    {
        var listPabrik = _pabrikRepo.ListData()?.ToList() ?? [];
        var response = listPabrik
            .OrderBy(x => x.PabrikName)
            .Select(x => new PabrikListResponse(x.PabrikId, x.PabrikName));

        return Task.FromResult(response);
    }
}