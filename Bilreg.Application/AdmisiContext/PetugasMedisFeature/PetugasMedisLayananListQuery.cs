using Ardalis.GuardClauses;
using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PetugasMedisFeature;

public record PetugasMedisLayananListQuery(string LayananId) : IRequest<IEnumerable<PetugasMedisLayananListResponse>>, ILayananKey ;

public record PetugasMedisLayananListResponse(
    string LayananId, string LayananName,
    IEnumerable<PetugasMedisLayananDokterResponse> ListDokter);

public record PetugasMedisLayananDokterResponse(
    string DokterId, string DokterName);

public class PetugasMedisLayananListHandler : IRequestHandler<PetugasMedisLayananListQuery, IEnumerable<PetugasMedisLayananListResponse>>
{
    private readonly IPetugasMedisRepo _petugasMedisRepo;
    private readonly IGetSatuanTugasMedisService _getSatTugasMedisSvc;
    public PetugasMedisLayananListHandler(IPetugasMedisRepo petugasMedisRepo, 
        IGetSatuanTugasMedisService getSatTugasMedisSvc)
    {
        _petugasMedisRepo = petugasMedisRepo;
        _getSatTugasMedisSvc = getSatTugasMedisSvc;
    }

    public Task<IEnumerable<PetugasMedisLayananListResponse>> Handle(PetugasMedisLayananListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        var satTgsMed = _getSatTugasMedisSvc.Execute();
        var instalasiDK = InstalasiDkType.Key("2");

        var listPtgMds = _petugasMedisRepo.ListData(satTgsMed, instalasiDK)?.ToList() ??
            throw new KeyNotFoundException("data not found");

        var result = listPtgMds.Where(x => x.fs_kd_layanan == request.LayananId)
            .GroupBy(y => new { y.fs_kd_layanan, y.fs_nm_layanan })
            .Select(g => new PetugasMedisLayananListResponse
                (
                    g.Key.fs_kd_layanan, g.Key.fs_nm_layanan,
                    g.Select(j => new PetugasMedisLayananDokterResponse(
                        j.fs_kd_peg, j.fs_nm_peg))
                ));
        return Task.FromResult(result);
    }
}
