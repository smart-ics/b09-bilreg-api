using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using MediatR;
namespace Bilreg.Application.BillContext.RoomChargeSub.KamarAgg;

public record KamarListDataByKelasId(string KelasId):IRequest<IEnumerable<KamarListByKelasResponse>>,IKelasKey;
public record KamarListByKelasResponse
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

public class KamarListByKelasHandler : IRequestHandler<KamarListDataByKelasId, IEnumerable<KamarListByKelasResponse>>
{
    private readonly IKamarDal _kamarDal;

    public KamarListByKelasHandler(IKamarDal kamarDal)
    {
        _kamarDal = kamarDal;
    }

    public Task<IEnumerable<KamarListByKelasResponse>> Handle(KamarListDataByKelasId request, CancellationToken cancellationToken)
    {
        var kamar = _kamarDal.ListData(request)
                    ?? throw new KeyNotFoundException($"Data Kamar not Found");
        
        var response = kamar.Select(x => new KamarListByKelasResponse(
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