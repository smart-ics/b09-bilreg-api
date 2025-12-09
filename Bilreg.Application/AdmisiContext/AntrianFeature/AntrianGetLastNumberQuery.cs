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

public record AntrianGetLastNumberQuery(string DokterHidokId, string TglAntrianYmd, string JamMulai) 
    : IRequest<AntrianGetLastNumberResponse>;


public record AntrianGetLastNumberResponse(int LastQueueNumber, int RemainingPatientQuota);


public class AntrianGetLastNumberHandler : IRequestHandler<AntrianGetLastNumberQuery, AntrianGetLastNumberResponse>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IPpaRepo _ppaRepo;
    public AntrianGetLastNumberHandler(IAntrianRepo antrianRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IPpaRepo ppaRepo)
    {
        _antrianRepo = antrianRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _ppaRepo = ppaRepo;
    }


    public Task<AntrianGetLastNumberResponse> Handle(AntrianGetLastNumberQuery request, CancellationToken cancellationToken)
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

        var sequenceTag = AntrianModel.GenSequenceTag(tglAntrian, dokter);
        var lastAntrian = GetLastAntrian(tglAntrian, dokter, sequenceTag);
        var jadwalThatDay = GetJadwalThatDay(dokter, tglAntrian);

        var lastQueueNumber = lastAntrian.NoUrut == -1 ? 0 : lastAntrian.NoUrut;
        var remainingPatientQuota = jadwalThatDay.MaxPasien - lastQueueNumber;

        // RETURN
        var result = new AntrianGetLastNumberResponse(lastQueueNumber, remainingPatientQuota);
        return Task.FromResult(result); 
    }
    #region PRIVATE_HELPER
    private AntrianEntryModel GetLastAntrian(DateOnly tglAntrian, PpaType dokter, string sequenceTag)
    {
        var listAntrianDb = _antrianRepo.ListData(tglAntrian)?.ToList()
            ?? throw new ArgumentException($"Antrian at {tglAntrian.ToString("yyyy-MM-dd")} not foud");

        var antrianHeader = listAntrianDb.Where(x => x.SequenceTag == sequenceTag).FirstOrDefault();
        var antrian = _antrianRepo.LoadEntity(antrianHeader!)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Antrian at {tglAntrian.ToString("yyyy-MM-dd")} not foud")
            );

        var result = antrian.ListEntry.OrderByDescending(x => x.NoUrut).FirstOrDefault() ?? AntrianEntryModel.Default;

        return result;

    }
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
