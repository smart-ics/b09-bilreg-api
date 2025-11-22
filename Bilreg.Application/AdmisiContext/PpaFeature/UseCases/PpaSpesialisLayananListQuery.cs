using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaSpesialisLayananListQuery() : IRequest<IEnumerable<PpaSpesialisLayanListResponse>>;

public record PpaSpesialisLayanListResponse(
    string GroupSpesialisId,
    string GroupSpesialisName, 
    IEnumerable<PpaSpesialisLayananDokterListResponse> ListDokter);

public record PpaSpesialisLayananDokterListResponse(
    string LayananId, 
    string LayananName,
    string DokterId,
    string DokterName);

public class PpaLayananHandler : IRequestHandler<PpaSpesialisLayananListQuery, IEnumerable<PpaSpesialisLayanListResponse>>
{
    private readonly IGetSatuanTugasMedisService _getSatTugasMedisSvc;
    private readonly IPpaRepo _ppaRepo;
    public PpaLayananHandler(IGetSatuanTugasMedisService getSatTugasMedisSvc, 
        IPpaRepo ppaRepo)
    {
        _getSatTugasMedisSvc = getSatTugasMedisSvc;
        _ppaRepo = ppaRepo;
    }

    public Task<IEnumerable<PpaSpesialisLayanListResponse>> Handle(PpaSpesialisLayananListQuery request, CancellationToken cancellationToken)
    {
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
