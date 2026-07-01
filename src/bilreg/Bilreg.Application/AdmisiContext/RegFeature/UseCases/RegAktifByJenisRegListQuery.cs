using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegAktifByJenisRegListQuery(int JenisReg) : IRequest<IEnumerable<RegAktifByJenisRegListResponse>>;
public record RegAktifByJenisRegListResponse(string RegId, string RegDate, string PasienId,
    string PasienName, string JenisReg, string JenisRegString, string LayananId,
    string LayananName, string DokterId, string DokterName);

public class RegAktifByJenisRegListHandler : IRequestHandler<RegAktifByJenisRegListQuery,IEnumerable<RegAktifByJenisRegListResponse>>
{
    private readonly IRegAktifRepo _regAktifRepo;

    public RegAktifByJenisRegListHandler(IRegAktifRepo regAktifRepo)
    {
        _regAktifRepo = regAktifRepo;
    }

    public Task<IEnumerable<RegAktifByJenisRegListResponse>> Handle(RegAktifByJenisRegListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request.JenisReg, nameof(request.JenisReg));

        var data = _regAktifRepo.ListData() ?? [];
        var dataByJenisReg = data
            .Where(x => x.JenisReg == (JenisRegEnum)request.JenisReg)?.ToList() ?? [];
        var result = dataByJenisReg
            .Select(x => new RegAktifByJenisRegListResponse(
                x.RegId,
                x.RegDate.ToString("yyyy-MM-dd"),
                x.Pasien.PasienId,
                x.Pasien.PasienName,
                ((int)x.JenisReg).ToString(),
                x.JenisReg.ToString(),
                x.Layanan.LayananId,
                x.Layanan.LayananName,
                x.Dokter.PpaId,
                x.Dokter.PpaName
            ));

        return Task.FromResult(result);
    }
}