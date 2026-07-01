using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public record ProsedurMasukInapListQuery() : IRequest<IEnumerable<ProsedurMasukInapListResponse>>;
public record ProsedurMasukInapListResponse(
    string ProsedurMasukInapId, string ProsedurMasukInapName);

public class ProsedurMasukInapListHandler : IRequestHandler<ProsedurMasukInapListQuery, IEnumerable<ProsedurMasukInapListResponse>>
{
    private readonly IProsedureMasukInapRepo _prosedurMasukInapRepo;

    public ProsedurMasukInapListHandler(IProsedureMasukInapRepo prosedurMasukInapRepo)
    {
        _prosedurMasukInapRepo = prosedurMasukInapRepo;
    }

    public Task<IEnumerable<ProsedurMasukInapListResponse>> Handle(ProsedurMasukInapListQuery request, CancellationToken cancellationToken)
    {
        var listData = _prosedurMasukInapRepo.ListData() ?? [];
        var result = listData.Select(x => new ProsedurMasukInapListResponse(
            x.ProsedurMasukInapId, x.ProsedurMasukInapName));
        return Task.FromResult(result);
    }
}
