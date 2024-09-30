using Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.KamarAgg;

public record KamarListDataByBangsalId(string BangsalId) : IRequest<IEnumerable<KamarListByBangsalResponse>>, IBangsalKey;

public record KamarListByBangsalResponse
(
    string KamarId,
    string KamarName,
    string Ket1,
    string Ket2,
    string Ket3,
    int JumlahKamar,
    int JumlahKamarPakai,
    int JumlahKamarKotor,
    int JumlahKamarRusak,
    string BangsalId,
    string BangsalName,
    string KelasId,
    string KelasName
);

public class KamarListByBangsalHandler : IRequestHandler<KamarListDataByBangsalId, IEnumerable<KamarListByBangsalResponse>>
{
    private readonly IKamarDal _kamarDal;

    public KamarListByBangsalHandler(IKamarDal kamarDal)
    {
        _kamarDal = kamarDal;
    }

    public Task<IEnumerable<KamarListByBangsalResponse>> Handle(KamarListDataByBangsalId request, CancellationToken cancellationToken)
    {
        var kamar = _kamarDal.ListData(request)
                    ?? throw new KeyNotFoundException($"Data Kamar not Found");
        
        var response = kamar.Select(x => new KamarListByBangsalResponse(
            x.KamarId,
            x.KamarName,
            x.Ket1,
            x.Ket2,
            x.Ket3,
            x.JumlahKamar,
            x.JumlahKamarPakai,
            x.JumlahKamarKotor,
            x.JumlahKamarRusak,
            x.BangsalId,
            x.BangsalName,
            x.KelasId,
            x.KelasName
            ));
        return Task.FromResult(response);
    }
}