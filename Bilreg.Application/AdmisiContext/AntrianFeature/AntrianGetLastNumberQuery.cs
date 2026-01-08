using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record AntrianGetQuotaQuery(string DokterHidokId, string TglAntrianYmd, string JamMulai) 
    : IRequest<AntrianGetQuotaResponse>;


public record AntrianGetQuotaResponse(int Quota, int Used, int AvailableQuota);


public class AntrianGetQuotaHandler : IRequestHandler<AntrianGetQuotaQuery, AntrianGetQuotaResponse>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IPpaRepo _ppaRepo;
    public AntrianGetQuotaHandler(IAntrianRepo antrianRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IPpaRepo ppaRepo)
    {
        _antrianRepo = antrianRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _ppaRepo = ppaRepo;
    }


    public Task<AntrianGetQuotaResponse> Handle(AntrianGetQuotaQuery request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrEmpty(request.DokterHidokId);
        Guard.Against.InvalidDateFormat(request.TglAntrianYmd, nameof(request.TglAntrianYmd));
        Guard.Against.InvalidTimeFormat(request.JamMulai, nameof(request.JamMulai));

        var finder = new ContactFinder(JenisContactEnum.Email, request.DokterHidokId);
        var dokter = _ppaRepo.LoadEntity(finder)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokter {request.DokterHidokId} not found")
            );

        // BUILD
        DateOnly tglAntrian = DateOnly.ParseExact(request.TglAntrianYmd,
            "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var jamPraktek = TimeOnly.Parse(request.JamMulai);

        var sequenceTag = AntrianModel.GenSequenceTag(tglAntrian, jamPraktek, dokter);
        // listAntrian
        var listAntrianDb = _antrianRepo.ListData(tglAntrian)?.ToList()
            ?? throw new ArgumentException($"Antrian at {tglAntrian.ToString("yyyy-MM-dd")} not foud");
        var antrianHeader = listAntrianDb.Where(x => x.SequenceTag == sequenceTag).FirstOrDefault();
        var antrian = _antrianRepo.LoadEntity(antrianHeader!)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Antrian at {tglAntrian.ToString("yyyy-MM-dd")} not foud")
            );
        // --

        var jadwalThatDay = GetJadwalThatDay(dokter, tglAntrian);

        var used = antrian.ListEntry.Count();
        var available = jadwalThatDay.MaxPasien - antrian.ListEntry.Count();

        // RETURN
        var result = new AntrianGetQuotaResponse(jadwalThatDay.MaxPasien, used, available);
        return Task.FromResult(result); 
    }
    #region PRIVATE_HELPER
    
    private JadwalPraktekType GetJadwalThatDay(PpaType dokter, DateOnly tglAntrian)
    {
        var jadwals = _jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var result = jadwals
            .Where(x => x.Hari == tglAntrian.DayOfWeek).FirstOrDefault()
            ?? throw new KeyNotFoundException($"Tidak ada jadwal atas dokter {dokter.PpaId} di tanggal " +
            $"{tglAntrian.ToString("yyyy-MM-dd")}");
        return result;
    }
    #endregion
}
