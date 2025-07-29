using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

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
    {
        var result = _groupJaminanDal
            .ListData();
        if (result is null)
            throw new KeyNotFoundException("GroupJaminan not found");
        var response = result.Select(x =>
            new GrupJaminanListResponse(
                x.GroupJaminanId, x.GroupJaminanName,
                x.IsKaryawan, x.Keterangan));
        return Task.FromResult(response);
    }
}