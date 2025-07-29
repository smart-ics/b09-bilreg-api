using Bilreg.Domain.AdmisiContext.JaminanSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

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
    {
        // QUERY
        var jaminan = _jaminanDal
            .GetData(request).Value;
        if (jaminan is null)
            throw new KeyNotFoundException($"Jaminan {request.JaminanId} not found");
        // RESPONSE
        var response = new JaminanGetResponse(
            jaminan.JaminanId, jaminan.JaminanName, jaminan.Alamat,
            jaminan.IsAktif, jaminan.CaraBayarDk, jaminan.GroupJaminan);
        return Task.FromResult(response);
    }
}