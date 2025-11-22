using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListDokterByGroupSpesialisQuery() : IRequest<IEnumerable<PpaListDokterByGroupSpesialisResponseDokter>>;

public record PpaListDokterByGroupSpesialisResponse(
    string GroupSpesialisId,
    string GroupSpesialisName, 
    IEnumerable<PpaListDokterByGroupSpesialisResponseDokter> ListDokter);

public record PpaListDokterByGroupSpesialisResponseDokter(
    string LayananId, 
    string LayananName,
    string DokterId,
    string DokterName);

public class PpaLayananHandler : IRequestHandler<PpaListDokterByGroupSpesialisQuery, IEnumerable<PpaListDokterByGroupSpesialisResponseDokter>>
{
    private readonly IPpaRepo _ppaRepo;
    public PpaLayananHandler(IPpaRepo ppaRepo)
    {
        _ppaRepo = ppaRepo;
    }

    public Task<IEnumerable<PpaListDokterByGroupSpesialisResponseDokter>> Handle(PpaListDokterByGroupSpesialisQuery request, CancellationToken cancellationToken)
    {
        var listDokter = _ppaRepo.ListData()
        // var satTgsMed = _getSatTugasMedisSvc.Execute();
        // var instalasiDK = InstalasiDkType.Key("2");
        //
        // var listPtgMds = _ppaRepo.ListData(satTgsMed, instalasiDK)?.ToList() ??
        //     throw new KeyNotFoundException("data not found");
        //
        // var result = listPtgMds
        //              .GroupBy(x => new { x.GroupSpesialisId, x.GroupSpesialisName })
        //              .Select(g => new PpaSpesialisLayanListResponse
        //              (
        //                  g.Key.GroupSpesialisId,
        //                  g.Key.GroupSpesialisName,
        //                  g.OrderBy(j => j.fs_nm_layanan)
        //                  .Select(j => new PpaSpesialisLayananDokterListResponse(
        //                      j.LayananId,
        //                      j.fs_nm_layanan,
        //                      j.PpaId,
        //                      j.fs_nm_peg))
        //              ));
        // return Task.FromResult(result);
        throw new NotImplementedException();
    }
}
