using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;

namespace Bilreg.Application.SalesContext.ResepFeature.UseCases;

public record ResepListByRegQuery(string RegId): IRequest<IEnumerable<ResepListByRegResponse>>;

public record ResepListByRegResponse(
    string ResepId,
    string RegId,
    string PasienId,
    string PasienName,
    string DokterId,
    string DokterName,
    string LayananId,
    string LayananName,
    int Iter
);

public class ResepListHandler: IRequestHandler<ResepListByRegQuery, IEnumerable<ResepListByRegResponse>>
{
    private readonly IResepRepo _resepRepo;

    public ResepListHandler(IResepRepo resepRepo)
    {
        _resepRepo = resepRepo;
    }

    public Task<IEnumerable<ResepListByRegResponse>> Handle(ResepListByRegQuery request, CancellationToken cancellationToken)
    {
        var regKey = RegModel.Key(request.RegId);
        var listResep = _resepRepo.ListData(regKey);
        var response = listResep.Select(GenResponseItem);
        return Task.FromResult(response);
    }

    private static ResepListByRegResponse GenResponseItem(ResepModel resep)
    {
        var reg = resep.Register;
        var dokter = resep.Dokter;
        var layanan = resep.Layanan;

        var result = new ResepListByRegResponse(
            resep.ResepId,
            reg.RegId,
            reg.PasienId,
            reg.PasienName,
            dokter.DokterId,
            dokter.DokterName,
            layanan.LayananId,
            layanan.LayananName,
            resep.Iter
        );

        return result;
    }
}