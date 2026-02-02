using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RujukanFeature;

public record RujukanListByCaraMasukDkQuery(string CaraMasukDkId) : 
    IRequest<IEnumerable<RujukanListByCaraMasukDkResponse>>, ICaraMasukDkKey;
public record RujukanListByCaraMasukDkResponse(
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

public class RujukanListByCaraMasukDkHandler : IRequestHandler<RujukanListByCaraMasukDkQuery, IEnumerable<RujukanListByCaraMasukDkResponse>>
{
    private readonly IRujukanRepo _rujukanRepo;

    public RujukanListByCaraMasukDkHandler(IRujukanRepo rujukanRepo)
    {
        _rujukanRepo = rujukanRepo;
    }

    public Task<IEnumerable<RujukanListByCaraMasukDkResponse>> Handle(RujukanListByCaraMasukDkQuery request, CancellationToken cancellationToken)
    {
        var listDto = _rujukanRepo.ListData(CaraMasukDkType.Key(request.CaraMasukDkId))?.ToList()
            ?? [];

        var response = listDto.Select(x => new RujukanListByCaraMasukDkResponse(
            x.RujukanId, x.RujukanName, x.IsAktif, x.PpkId,
            x.Alamat, x.Alamat.Kota, x.TipeRujukan.TipeRujukanId,
            x.TipeRujukan.TipeRujukanName, x.KelasRujukan.KelasRujukanId, x.KelasRujukan.KelasRujukanName,
            x.CaraMasukDk.CaraMasukDkId, x.CaraMasukDk.CaraMasukDkName));
        return Task.FromResult(response);
    }
}