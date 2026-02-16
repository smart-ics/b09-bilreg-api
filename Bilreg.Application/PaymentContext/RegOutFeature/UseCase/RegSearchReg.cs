using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.RegOutFeature.UseCase;

public record RegSearchReg(string Keyword) : IRequest<IEnumerable<RegSearchRegResponse>>;
public record RegSearchRegResponse(
    string RegId,
    string RegDate,
    string PasienId,
    string PasienName,
    string TipeJaminanName,
    string LayananName,
    string JenisReg,
    string JenisRegString);

public class RegSearchRegHandler : IRequestHandler<RegSearchReg, IEnumerable<RegSearchRegResponse>>
{
    private readonly IRegRepo _regRepo;
    private readonly IPasienRepo _pasienRepos;

    public RegSearchRegHandler(
        IRegRepo regRepo,
        IPasienRepo pasienRepo)
    {
        _regRepo = regRepo;
        _pasienRepos = pasienRepo;
    }

    public async Task<IEnumerable<RegSearchRegResponse>> Handle(RegSearchReg request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            return Array.Empty<RegSearchRegResponse>();
        }

        // Ambil semua registrasi
        var allRegs = _regRepo GetAllAsync(cancellationToken);

        // Buat RegFinder instances untuk semua registrasi
        var regFinders = allRegs.Select(reg => CreateRegFinder(reg)).ToList();

        // Filter yang cocok dengan keyword
        var matchedFinders = regFinders
            .Where(finder => finder.MatchesKeyword(request.Keyword))
            .ToList();

        // Sort by similarity score (descending)
        var sortedFinders = matchedFinders
            .OrderByDescending(finder => finder.CalculateSimilarityScore(request.Keyword))
            .ToList();

        // Convert to response
        var responses = new List<RegSearchRegResponse>();

        foreach (var finder in sortedFinders)
        {
            var reg = allRegs.First(r => r.RegId == finder.RegId);

            var pasien = await _pasienRepo.GetByIdAsync(reg.PasienId, cancellationToken);

            var response = new RegSearchRegResponse(
                RegId: reg.RegId,
                RegDate: reg.TglReg.ToString("yyyy-MM-dd"),
                PasienId: finder.PasienId,
                PasienName: finder.PasienName,
                TipeJaminanName: tipeJaminan?.NamaTipeJaminan ?? "-",
                LayananName: layanan?.LayananName ?? "-",
                JenisReg: reg.JenisReg.ToString(),
                JenisRegString: reg.JenisReg switch
                {
                    JenisRegEnum.RawatInap => "Rawat Inap",
                    JenisRegEnum.RawatJalan => "Rawat Jalan",
                    JenisRegEnum.GawatDarurat => "Gawat Darurat",
                    _ => "Tidak Diketahui"
                });

            responses.Add(response);
        }

        return responses;
    }

    private static RegFinder CreateRegFinder(RegModel reg)
    {
        return RegFinder.Create(
            regId: reg.RegId,
            pasienId: reg.PasienId,
            pasienName: reg.PasienName,
            bookingId: reg.BookingId);
    }
}