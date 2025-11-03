using Bilreg.Domain.AdmisiContext.JaminanSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;

public record JaminanGetQuery(string JaminanId) : IRequest<JaminanGetResponse>, IJaminanKey;

public record JaminanGetResponse(
    string JaminanId,
    string JaminanName,
    AlamatType Address,
    bool IsAktif,
    CaraBayarDkType CaraBayarDk,
    GroupJaminanReff GrupJaminan);

public class JaminanGetHandler : IRequestHandler<JaminanGetQuery, JaminanGetResponse>
{
    private readonly IJaminanDal _jaminanDal;

    public JaminanGetHandler(IJaminanDal jaminanDal)
    {
        _jaminanDal = jaminanDal;
    }

    public Task<JaminanGetResponse> Handle(JaminanGetQuery request, CancellationToken cancellationToken)
        => _jaminanDal.GetData(request)
        .Match(
            onSome: x => Task.FromResult(new JaminanGetResponse(x.JaminanId, x.JaminanName, x.Alamat,
                x.IsAktif, x.CaraBayarDk, x.GroupJaminan)),
            onNone: () => throw new KeyNotFoundException($"Jaminan {request.JaminanId} not found"));
}