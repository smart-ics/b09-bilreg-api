using Bilreg.Domain.BillContext.RoomChargeSub.KamarAgg;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.KamarAgg;

public record KamarGetQuery(string KamarId):IRequest<KamarGetResponse>,IKamarKey;
public record KamarGetResponse(
    string KamarId,
    string KamarName,
    string Ket1,
    string Ket2,
    string Ket3,
    decimal JumlahKamar,
    decimal JumlahKamarPakai,
    decimal JumlahKamarKotor,
    decimal JumlahKamarRusak,
    string BangsalId,
    string BangsalName,
    string KelasId,
    string KelasName
    );

public class KamarGetHandler : IRequestHandler<KamarGetQuery, KamarGetResponse>
{
    private readonly IKamarDal _kamarDal;

    public KamarGetHandler(IKamarDal kamarDal)
    {
        _kamarDal = kamarDal;
    }

    public Task<KamarGetResponse> Handle(KamarGetQuery request, CancellationToken cancellationToken)
    {
        var kamar = _kamarDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kamar id {request.KamarId} not found");

        var response = new KamarGetResponse(
            kamar.KamarId,
            kamar.KamarName,
            kamar.Ket1,
            kamar.Ket2,
            kamar.Ket3,
            kamar.JumlahKamar,
            kamar.JumlahKamarPakai,
            kamar.JumlahKamarKotor,
            kamar.JumlahKamarRusak,
            kamar.BangsalId,
            kamar.BangsalName,
            kamar.KelasId,
            kamar.KelasName
        );
        
        return Task.FromResult(response);
    }
}