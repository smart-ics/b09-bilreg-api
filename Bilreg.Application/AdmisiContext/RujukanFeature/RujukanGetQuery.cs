using Bilreg.Domain.AdmisiContext.RujukanSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RujukanFeature;
public record RujukanGetQuery(string RujukanId) : IRequest<RujukanGetResponse>, IRujukanKey;
public record RujukanGetResponse(
    string RujukanId,
    string RujukanName,
    bool IsAktif,
    AlamatType Alamat,
    string Telepon,
    string RujukanTipeId,
    string RujukanTipeName,
    string KelasId,
    string KelasName,
    string CaraMasukDkId,
    string CaraMasukDkName
);
public class RujukanGetHandler : IRequestHandler<RujukanGetQuery, RujukanGetResponse>
{
    private readonly IRujukanRepo _rujukanRepo;

    public RujukanGetHandler(IRujukanRepo rujukanRepo)
    {
        _rujukanRepo = rujukanRepo;
    }
    public Task<RujukanGetResponse> Handle(RujukanGetQuery request, CancellationToken cancellationToken)
        => _rujukanRepo.LoadEntity(RujukanType.Key(request.RujukanId))
        .Match(
            onSome: x => Task.FromResult(new RujukanGetResponse(x.RujukanId, x.RujukanName, x.IsAktif,
                x.Alamat, x.Alamat.Kota, x.TipeRujukan.TipeRujukanId,
                x.TipeRujukan.TipeRujukanName, x.KelasRujukan.KelasRujukanId, x.KelasRujukan.KelasRujukanName,
                x.CaraMasukDk.CaraMasukDkId, x.CaraMasukDk.CaraMasukDkName)),
            onNone: () => throw new KeyNotFoundException($"Rujukan {request.RujukanId} not found"));
    
}