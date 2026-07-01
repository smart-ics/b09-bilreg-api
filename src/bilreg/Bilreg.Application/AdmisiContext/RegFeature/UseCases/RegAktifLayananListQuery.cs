using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegAktifLayananListQuery(string LayananId) : IRequest<IEnumerable<RegAktifLayananListResponse>>;

public record RegAktifLayananListResponse(string RegId, string RegDate, string PasienId,
    string PasienName, string JenisReg, string JenisRegString, string LayananId, 
    string LayananName, string DokterId, string DokterName);

public class RegAktifListHandler : IRequestHandler<RegAktifLayananListQuery, IEnumerable<RegAktifLayananListResponse>>
{
    private readonly IRegAktifRepo _regAktifRepo;

    public RegAktifListHandler(IRegAktifRepo regAktifRepo)
    {
        _regAktifRepo = regAktifRepo;
    }

    public Task<IEnumerable<RegAktifLayananListResponse>> Handle(RegAktifLayananListQuery request, CancellationToken cancellationToken)
    {
        var periode = new Periode(DateTime.Now);
        var listRegLayanan = _regAktifRepo.ListData(LayananType.Key(request.LayananId))?.ToList() ?? [];
        var result = listRegLayanan
            .Select(x => new RegAktifLayananListResponse(
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
