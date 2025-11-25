using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;

public record JaminanListQuery() : IRequest<IEnumerable<JaminanListResponse>>;

public record JaminanListResponse(
    string JaminanId,
    string JaminanName,
    string CaraBayarDkName,
    string GrupJaminanName);

public class JaminanListHandler : IRequestHandler<JaminanListQuery, IEnumerable<JaminanListResponse>>
{
    private readonly IJaminanRepo _jaminanRepo;

    public JaminanListHandler(IJaminanRepo jaminanRepo)
    {
        _jaminanRepo = jaminanRepo;
    }

    public Task<IEnumerable<JaminanListResponse>> Handle(JaminanListQuery request, CancellationToken cancellationToken)
    {
        var listJaminan = _jaminanRepo.ListData()?.ToList() ?? [];
        var result = listJaminan
            .Select(x => new JaminanListResponse
            (x.JaminanId, x.JaminanName, x.CaraBayarDk.CaraBayarDkName, x.GroupJaminan.GroupJaminanName));
        return Task.FromResult(result); 
    }
}