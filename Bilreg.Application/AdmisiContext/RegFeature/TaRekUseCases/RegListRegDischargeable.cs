using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.TaRekUseCases;

public record RegListRegDischargeable() : IRequest<IEnumerable<RegListRegDischargeableResponse>>;

public record RegListRegDischargeableResponse(string RegId, string RegDate, 
    string PasienId, string PasienName, string TipeJaminanName, string LayananName,
    string JenisRegString);

public class RegListRegDischargeableHandler : IRequestHandler<RegListRegDischargeable, IEnumerable<RegListRegDischargeableResponse>>
{
    private readonly IRegAktifRepo _regAktifRepo;
    public RegListRegDischargeableHandler(IRegAktifRepo regAktifRepo)
    {
        _regAktifRepo = regAktifRepo;
    }

    public Task<IEnumerable<RegListRegDischargeableResponse>> Handle(RegListRegDischargeable request, CancellationToken cancellationToken)
    {
        var listRegDischargeable = _regAktifRepo.ListData()?.ToList() ?? [];

        // masih perlu tambah guard untuk px inap yang tidak aktif bed
        var result = listRegDischargeable
            .Select(x => new RegListRegDischargeableResponse(
                x.RegId,
                x.RegDate.ToString("yyyy-MM-dd"),
                x.Pasien.PasienId,
                x.Pasien.PasienName,
                x.TipeJaminan.TipeJaminanName,
                x.Layanan.LayananName,
                ((int)x.JenisReg).ToString()
            ));

        return Task.FromResult(result);
    }
}