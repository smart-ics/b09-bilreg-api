using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RujukanFeature;

public record RjkGetByPpkIdQuery(string PpkId) :IRequest<RjkGetByPpkIdResponse>, IPpkKey ;

public record RjkGetByPpkIdResponse(
    string RujukanId,
    string RujukanName,
    bool IsAktif,
    string PpkId,
    AlamatType Alamat,
    string Telepon,
    string RujukanTipeId,
    string RujukanTipeName,
    string KelasId,
    string KelasName,
    string CaraMasukDkId,
    string CaraMasukDkName
    );
public class RjkGetByPpkIdHandler : IRequestHandler<RjkGetByPpkIdQuery, RjkGetByPpkIdResponse>
{
    private readonly IRujukanRepo _rujukanRepo;

    public RjkGetByPpkIdHandler(IRujukanRepo rujukanRepo)
    {
        _rujukanRepo = rujukanRepo;
    }

    public Task<RjkGetByPpkIdResponse> Handle(RjkGetByPpkIdQuery request, CancellationToken cancellationToken)
    {
        var rjk = _rujukanRepo.LoadEntity(request).GetValueOrThrow($"Rujukan by Ppk {request.PpkId} not found");
        var result = new RjkGetByPpkIdResponse(rjk.RujukanId, rjk.RujukanName, rjk.IsAktif,
                rjk.PpkId, rjk.Alamat, rjk.Alamat.Kota, rjk.TipeRujukan.TipeRujukanId,
                rjk.TipeRujukan.TipeRujukanName, rjk.KelasRujukan.KelasRujukanId, rjk.KelasRujukan.KelasRujukanName,
                rjk.CaraMasukDk.CaraMasukDkId, rjk.CaraMasukDk.CaraMasukDkName);
        return Task.FromResult(result);
    }
}
