using Bilreg.Application.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.RegOutFeature.UseCase;

public record RegListRegDischargeableQuery() : IRequest<IEnumerable<RegListRegDischargeableResponse>>;
public record RegListRegDischargeableResponse(string RegId, string RegDate,
    string PasienId, string PasienName, string TipeJaminanName,
    string LayananName, string JenisReg, string JenisRegString);

public class RegListRegDischargeableHandler : IRequestHandler<RegListRegDischargeableQuery, IEnumerable<RegListRegDischargeableResponse>>
{
    private readonly IRegAktifRepo _regAktifRepo;
    public RegListRegDischargeableHandler(IRegAktifRepo regAktifRepo)
    {
        _regAktifRepo = regAktifRepo;
    }

    public Task<IEnumerable<RegListRegDischargeableResponse>> Handle(RegListRegDischargeableQuery request, CancellationToken cancellationToken)
    {
        var listRegAktif = _regAktifRepo.ListData()?.ToList() ?? [];

        // Belum ada guard aktif pakai BED, jadi sementara pakai semua data reg aktif
        var result = listRegAktif
            .Select(x => new RegListRegDischargeableResponse(
                x.RegId,
                x.RegDate.ToString("yyyy-MM-dd"),
                x.Pasien.PasienId,
                x.Pasien.PasienName,
                x.TipeJaminan.TipeJaminanName,
                x.Layanan.LayananName,
                ((int)x.JenisReg).ToString(),
                x.JenisReg.ToString()
            ));

        return Task.FromResult(result);
    }
}
