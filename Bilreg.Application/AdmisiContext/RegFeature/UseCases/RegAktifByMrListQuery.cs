using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegAktifByMrListQuery(string PasienId) : IRequest<IEnumerable<RegAktifByMrListResponse>>, IPasienKey;

public record RegAktifByMrListResponse(string RegId, string RegDate, string PasienId,
    string PasienName, string JenisReg, string JenisRegString, string LayananId,
    string LayananName, string DokterId, string DokterName);

public class RegAktifByMrListHandler : IRequestHandler<RegAktifByMrListQuery, IEnumerable<RegAktifByMrListResponse>>
{
    private readonly IRegAktifRepo _regAktifRepo;

    public RegAktifByMrListHandler(IRegAktifRepo regAktifRepo)
    {
        _regAktifRepo = regAktifRepo;
    }

    public Task<IEnumerable<RegAktifByMrListResponse>> Handle(RegAktifByMrListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId, nameof(request.PasienId));

        var listRegAktif = _regAktifRepo.ListData(request)?.ToList() ?? [];
        var result = listRegAktif
            .Select(x => new RegAktifByMrListResponse(
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
