using Bilreg.Domain.AdmisiContext.JaminanSub;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

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
    {
        // QUERY
        var result = _groupJaminanDal
            .GetData(request);
        if (result is null)
            throw new KeyNotFoundException($"GroupJaminan {request.GroupJaminanId} not found");

        // RESPONSE
        var response = new GrupJaminanGetResponse(
            result.GroupJaminanId, result.GroupJaminanName,
            result.IsKaryawan, result.Keterangan);
        return Task.FromResult(response);
    }
}