using Bilreg.Domain.AdmisiContext.JaminanSub;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.GrupJaminanAgg;

public record GrupJaminanGetQuery(string GroupJaminanId) : IRequest<GrupJaminanGetResponse>, IGroupJaminanKey;

public record GrupJaminanGetResponse(
    string GrupJaminanId,
    string GrupJaminanName,
    bool IsKaryawan,
    string Keterangan);

public class GrupJaminanGetHandler : IRequestHandler<GrupJaminanGetQuery, GrupJaminanGetResponse>
{
    private readonly IGroupJaminanDal _groupJaminanDal;

    public GrupJaminanGetHandler(IGroupJaminanDal groupJaminanDal)
    {
        _groupJaminanDal = groupJaminanDal;
    }

    public Task<GrupJaminanGetResponse> Handle(GrupJaminanGetQuery request, CancellationToken cancellationToken)
        => _groupJaminanDal.GetData(request)
        .Match(
            onSome: x => Task.FromResult(new GrupJaminanGetResponse(x.GroupJaminanId, x.GroupJaminanName, x.IsKaryawan, x.Keterangan)),
            onNone: () => throw new KeyNotFoundException($"GroupJaminan {request.GroupJaminanId} not found"));
}