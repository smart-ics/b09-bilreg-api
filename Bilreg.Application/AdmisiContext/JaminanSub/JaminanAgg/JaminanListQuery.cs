using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanListQuery() : IRequest<IEnumerable<JaminanListResponse>>;

public record JaminanListResponse(
    string JaminanId,
    string JaminanName,
    string CaraBayarDkName,
    string GrupJaminanName);

public class JaminanListHandler : IRequestHandler<JaminanListQuery, IEnumerable<JaminanListResponse>>
{
    private readonly IJaminanDal _jaminanDal;

    public JaminanListHandler(IJaminanDal jaminanDal)
    {
        _jaminanDal = jaminanDal;
    }

    public Task<IEnumerable<JaminanListResponse>> Handle(JaminanListQuery request, CancellationToken cancellationToken)
        => _jaminanDal.ListData()
        .Match(
            onSome: x => Task.FromResult(x.Select(y
                => new JaminanListResponse(y.JaminanId, y.JaminanName, y.CaraBayarDk.CaraBayarDkName, y.GroupJaminan.GroupJaminanName))),
            onNone: () => throw new KeyNotFoundException("Jaminan not found"));
}