using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.GrupJaminanAgg;

public record GrupJaminanListQuery() : IRequest<IEnumerable<GrupJaminanListResponse>>;

public record GrupJaminanListResponse(
    string GrupJaminanId,
    string GrupJaminanName,
    bool IsKaryawan,
    string Keterangan
    );

public class GrupJaminanListHandler : IRequestHandler<GrupJaminanListQuery, IEnumerable<GrupJaminanListResponse>>
{
    private readonly IGroupJaminanDal _groupJaminanDal;

    public GrupJaminanListHandler(IGroupJaminanDal groupJaminanDal)
    {
        _groupJaminanDal = groupJaminanDal;
    }

    public Task<IEnumerable<GrupJaminanListResponse>> Handle(GrupJaminanListQuery request, CancellationToken cancellationToken)
        => _groupJaminanDal.ListData()
        .Match(
            onSome: x => Task.FromResult(x.Select(y
                => new GrupJaminanListResponse(y.GroupJaminanId, y.GroupJaminanName, y.IsKaryawan, y.Keterangan))),
            onNone: () => throw new KeyNotFoundException("GroupJaminan not found"));
}