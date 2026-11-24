using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegAktifRajalIgdListQuery : IRequest<IEnumerable<RegAktifRajalIgdListResponse>>;

public record RegAktifRajalIgdListResponse(
    string RegId,
    string RegDate,
    string PasienId,
    string PasienName,
    string TglLahir,
    string Gender,
    string JenisReg,
    string JenisRegString,
    string LayananId,
    string LayananName,
    string DokterId,
    string DokterName,
    string TipeJaminanId,
    string TipeJaminanName,
    int StatusTataRekening = 0);

public class RegAktifRajalIgdListHandler : IRequestHandler<RegAktifRajalIgdListQuery, IEnumerable<RegAktifRajalIgdListResponse>>
{
    private readonly IRegAktifRepo _regAktifRepo;

    public RegAktifRajalIgdListHandler(IRegAktifRepo regAktifRepo)
    {
        _regAktifRepo = regAktifRepo;
    }

    public Task<IEnumerable<RegAktifRajalIgdListResponse>> Handle(
        RegAktifRajalIgdListQuery request,
        CancellationToken cancellationToken)
    {
        var datas = _regAktifRepo.ListDataRajalIgd() ?? [];
        var result = datas
            .Select(x => new RegAktifRajalIgdListResponse(
                x.RegId,
                x.RegDate.ToString("yyyy-MM-dd"),
                x.Pasien.PasienId,
                x.Pasien.PasienName,
                x.Pasien.TglLahir.ToString("yyyy-MM-dd"),
                x.Pasien.Gender,
                ((int)x.JenisReg).ToString(),
                x.JenisReg.ToString(),
                x.Layanan.LayananId,
                x.Layanan.LayananName,
                x.Dokter.PpaId,
                x.Dokter.PpaName,
                x.TipeJaminan.TipeJaminanId,
                x.TipeJaminan.TipeJaminanName,
                x.StatusTataRekening))
            .ToList();

        return Task.FromResult<IEnumerable<RegAktifRajalIgdListResponse>>(result);
    }
}
