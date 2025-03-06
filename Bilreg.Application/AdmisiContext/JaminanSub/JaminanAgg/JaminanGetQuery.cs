using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanGetQuery(string JaminanId) : IRequest<JaminanGetResponse>, IJaminanKey;

public record JaminanGetResponse(
    string JaminanId,
    string JaminanName,
    AddressType Address,
    bool IsAktif,
    CaraBayarDkModel CaraBayarDk,
    GrupJaminanViewType GrupJaminan,
    string BenefitMou);

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
            .GetData2(request)
            .OrThrowNotFoundException()
            .Value;

        // RESPONSE
        var response = new JaminanGetResponse(
            jaminan.JaminanId, jaminan.JaminanName, jaminan.Address,
            jaminan.IsAktif, jaminan.CaraBayarDk, jaminan.GrupJaminan,
            jaminan.BenefitMou);
        return Task.FromResult(response);
    }
}