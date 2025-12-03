using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegAktifListQuery() : IRequest<IEnumerable<RegAktifListResponse>>;

public record RegAktifListResponse(string RegId, string RegDate, string PasienId,
    string PasienName, string JenisReg, string JenisRegString, string LayananId, 
    string LayananName, string DokterId, string DokterName);

public class RegAktifListHandler : IRequestHandler<RegAktifListQuery, IEnumerable<RegAktifListResponse>>
{
    private readonly IRegAktifRepo _regAktifRepo;

    public RegAktifListHandler(IRegAktifRepo regAktifRepo)
    {
        _regAktifRepo = regAktifRepo;
    }

    public Task<IEnumerable<RegAktifListResponse>> Handle(RegAktifListQuery request, CancellationToken cancellationToken)
    {
        var periode = new Periode(DateTime.Now);
        var listRegAktif = _regAktifRepo.ListData(periode)?.ToList() ?? [];
        var result = listRegAktif
            .Select(x => new RegAktifListResponse(
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
