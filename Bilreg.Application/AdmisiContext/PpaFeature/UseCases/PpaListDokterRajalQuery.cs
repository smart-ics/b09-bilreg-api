using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListDokterRajalQuery() : IRequest<IEnumerable<PpaListDokterRajalResponse>>;

public record PpaListDokterRajalResponse(
    string GroupSpesialisId,
    string GroupSpesialisName, 
    IEnumerable<PpaListDokterRajalResponseDokter> ListDokter);

public record PpaListDokterRajalResponseDokter(
    string LayananId, 
    string LayananName,
    string DokterId,
    string DokterName);

public class PpaListDokterRajalHandler : IRequestHandler<PpaListDokterRajalQuery, IEnumerable<PpaListDokterRajalResponse>>
{
    private readonly IPpaRepo _ppaRepo;
    private readonly IGroupSpesialisRepo _groupSpesialisRepo;
    private readonly ILayananRepo _layananRepo;
    public PpaListDokterRajalHandler(IPpaRepo ppaRepo, 
        IGroupSpesialisRepo groupSpesialisRepo, 
        ILayananRepo layananRepo)
    {
        _ppaRepo = ppaRepo;
        _groupSpesialisRepo = groupSpesialisRepo;
        _layananRepo = layananRepo;
    }

    public Task<IEnumerable<PpaListDokterRajalResponse>> Handle(
        PpaListDokterRajalQuery request, CancellationToken cancellationToken)
    {
        var listLayanan = _layananRepo
            .ListData(InstalasiDkType.RawatJalan)?
            .ToList() ?? [];
        var listPpa = _ppaRepo.ListData(ProfesiType.Dokter, listLayanan)?.ToList() ?? [];
        var listGroupSpesialis = _groupSpesialisRepo.ListData()?.ToList() ?? [];
        var result = (from item in listGroupSpesialis
            let listLayananThisGrupSpesialis = listLayanan
                .Where(x => x.GroupSpesialis.GroupSpesialisId == item.GroupSpesialisId)
                .Select(x => x.LayananId)
                .ToList()
            let listDokter = listPpa
                .Where(x => listLayananThisGrupSpesialis.Any(y => y == x.Layanan.LayananId))
                .Select(x => new PpaListDokterRajalResponseDokter(x.Layanan.LayananId, x.Layanan.LayananName, x.PpaId, x.PpaName))
                .ToList()
            select new PpaListDokterRajalResponse(item.GroupSpesialisId, item.GroupSpesialisName, listDokter)).ToList();
        return Task.FromResult(result.AsEnumerable());
    }
}
