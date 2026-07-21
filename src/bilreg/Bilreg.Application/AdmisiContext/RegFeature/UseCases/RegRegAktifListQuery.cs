using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegRegAktifListQuery : IRequest<IEnumerable<RegRegAktifListResponse>>;

public record RegRegAktifListResponse(string RegId, string RegDate, string PasienId,
    string PasienName, string JenisReg, string JenisRegString, string LayananId,
    string LayananName, string DokterId, string DokterName);

public class RegRegAktifListHandler : IRequestHandler<RegRegAktifListQuery, IEnumerable<RegRegAktifListResponse>>
{
    private readonly IRegAktifRepo _regAktifRepo;

    public RegRegAktifListHandler(IRegAktifRepo regAktifRepo)
    {
        _regAktifRepo = regAktifRepo;
    }

    public Task<IEnumerable<RegRegAktifListResponse>> Handle(RegRegAktifListQuery request, CancellationToken cancellationToken)
    {
        var datas = _regAktifRepo.ListData() ?? [];
        var resutl = datas.Select(x => new RegRegAktifListResponse(
            x.RegId, x.RegDate.ToString("yyyy-MM-dd"), 
            x.Pasien.PasienId, x.Pasien.PasienName,
            ((int)x.JenisReg).ToString(), x.JenisReg.ToString(), 
            x.Layanan.LayananId, x.Layanan.LayananName, 
            x.Dokter.PpaId, x.Dokter.PpaName));

        return Task.FromResult(resutl);
    }
}