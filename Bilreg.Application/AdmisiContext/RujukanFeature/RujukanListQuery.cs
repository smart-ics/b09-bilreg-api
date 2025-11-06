using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RujukanFeature;
public record RujukanListQuery(string TipeRujukanId) : IRequest<IEnumerable<RujukanListResponse>>;
public record RujukanListResponse(
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

public class RujukanListHandler : IRequestHandler<RujukanListQuery, IEnumerable<RujukanListResponse>>
{
    private readonly IRujukanRepo _rujukanRepo;

    public RujukanListHandler(IRujukanRepo rujukanRepo)
    {
        _rujukanRepo = rujukanRepo;
    }

    public Task<IEnumerable<RujukanListResponse>> Handle(RujukanListQuery request, CancellationToken cancellationToken)
    {
        var listDto = _rujukanRepo.ListData(TipeRujukanType.Key(request.TipeRujukanId))?.ToList()
            ?? [];

        var response = listDto.Select(x => new RujukanListResponse(
            x.RujukanId, x.RujukanName, x.IsAktif,
            x.Alamat, x.Alamat.Kota, x.TipeRujukan.TipeRujukanId,
            x.TipeRujukan.TipeRujukanName, x.KelasRujukan.KelasRujukanId, x.KelasRujukan.KelasRujukanName,
            x.CaraMasukDk.CaraMasukDkId, x.CaraMasukDk.CaraMasukDkName));
        return Task.FromResult(response);
    }
}