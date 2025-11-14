using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PetugasMedisFeature;

public record PetugasMedisSpesialisLayananListQuery() : IRequest<IEnumerable<PetugasMedisSpesialisLayanListResponse>>;

public record PetugasMedisSpesialisLayanListResponse(
    string GroupSpesialisId,
    string GroupSpesialisName, 
    IEnumerable<PetugasMedisSpesialisLayananDokterListResponse> ListDokter);

public record PetugasMedisSpesialisLayananDokterListResponse(
    string LayananId, 
    string LayananName,
    string DokteId,
    string DokterName);

public class PetugasMededisLayananHandler : IRequestHandler<PetugasMedisSpesialisLayananListQuery, IEnumerable<PetugasMedisSpesialisLayanListResponse>>
{
    private readonly IGetSatuanTugasMedisService _getSatTugasMedisSvc;
    private readonly IPetugasMedisRepo _petugasMedisRepo;
    public PetugasMededisLayananHandler(IGetSatuanTugasMedisService getSatTugasMedisSvc, 
        IPetugasMedisRepo petugasMedisRepo)
    {
        _getSatTugasMedisSvc = getSatTugasMedisSvc;
        _petugasMedisRepo = petugasMedisRepo;
    }

    public Task<IEnumerable<PetugasMedisSpesialisLayanListResponse>> Handle(PetugasMedisSpesialisLayananListQuery request, CancellationToken cancellationToken)
    {
        var satTgsMed = _getSatTugasMedisSvc.Execute();
        var instalasiDK = InstalasiDkType.Key("2");

        var listPtgMds = _petugasMedisRepo.ListData(satTgsMed, instalasiDK)?.ToList() ??
            throw new KeyNotFoundException("data not found");

        var result = listPtgMds
                     .GroupBy(x => new { x.GroupSpesialisId, x.GroupSpesialisName })
                     .Select(g => new PetugasMedisSpesialisLayanListResponse
                     (
                         g.Key.GroupSpesialisId,
                         g.Key.GroupSpesialisName,
                         g.OrderBy(j => j.fs_nm_layanan)
                         .Select(j => new PetugasMedisSpesialisLayananDokterListResponse(
                             j.fs_kd_layanan,
                             j.fs_nm_layanan,
                             j.fs_kd_peg,
                             j.fs_nm_peg))
                     ));
        return Task.FromResult(result);
    }
}
