using Ardalis.GuardClauses;
using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaLayananListQuery(string LayananId) : IRequest<IEnumerable<PpaLayananListResponse>>, ILayananKey ;

public record PpaLayananListResponse(
    string LayananId, string LayananName,
    IEnumerable<PpaLayananDokterResponse> ListDokter);

public record PpaLayananDokterResponse(
    string DokterId, string DokterName);

public class PpaLayananListHandler : IRequestHandler<PpaLayananListQuery, IEnumerable<PpaLayananListResponse>>
{
    private readonly IPpaRepo _ppaRepo;
    private readonly IGetSatuanTugasMedisService _getSatTugasMedisSvc;
    public PpaLayananListHandler(IPpaRepo ppaRepo, 
        IGetSatuanTugasMedisService getSatTugasMedisSvc)
    {
        _ppaRepo = ppaRepo;
        _getSatTugasMedisSvc = getSatTugasMedisSvc;
    }

    public Task<IEnumerable<PpaLayananListResponse>> Handle(PpaLayananListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        var satTgsMed = _getSatTugasMedisSvc.Execute();
        var instalasiDK = InstalasiDkType.Key("2");

        var listPtgMds = _ppaRepo.ListData(satTgsMed, instalasiDK)?.ToList() ??
            throw new KeyNotFoundException("data not found");

        var result = listPtgMds.Where(x => x.LayananId == request.LayananId)
            .GroupBy(y => new { fs_kd_layanan = y.LayananId, y.fs_nm_layanan })
            .Select(g => new PpaLayananListResponse
                (
                    g.Key.fs_kd_layanan, g.Key.fs_nm_layanan,
                    g.Select(j => new PpaLayananDokterResponse(
                        j.PpaId, j.fs_nm_peg))
                ));
        return Task.FromResult(result);
    }
}
