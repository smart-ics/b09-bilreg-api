using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IDeleteBookingWorkflow
{
    Task Execute(IBookingKey bookKey);
}
public sealed class DeleteBookingWorkflow : IDeleteBookingWorkflow
{
    private readonly IBookingRepo _bookingRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IPasienTrackerRepo _pasienTrackerRepo;
    private readonly IAntrianMapRepo _antrianMapRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IJadwalPraktekHarianRepo _jadwalPraktekHarianRepo;
    private readonly IJadwalPraktekFeatureResolver _featureResolver;
    private readonly ITglJamProvider _tglJamProvider;

    public DeleteBookingWorkflow(
        IBookingRepo bookingRepo,
        IPpaRepo ppaRepo,
        IAntrianRepo antrianRepo,
        IPasienTrackerRepo pasienTrackerRepo,
        IAntrianMapRepo antrianMapRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IJadwalPraktekHarianRepo jadwalPraktekHarianRepo,
        IJadwalPraktekFeatureResolver featureResolver,
        ITglJamProvider tglJamProvider)
    {
        _bookingRepo = bookingRepo;
        _ppaRepo = ppaRepo;
        _antrianRepo = antrianRepo;
        _pasienTrackerRepo = pasienTrackerRepo;
        _antrianMapRepo = antrianMapRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _jadwalPraktekHarianRepo = jadwalPraktekHarianRepo;
        _featureResolver = featureResolver;
        _tglJamProvider = tglJamProvider;
    }

    public Task Execute(IBookingKey key)
    {
        var occurredAt = _tglJamProvider.Now;
        var booking = LoadBookingOrExit(key);
        if (booking is null)
            return Task.CompletedTask;

        EnsureBookingNotRegistered(booking);

        var dokter = LoadDokterOrThrow(booking);
        var antrianView = LoadAntrianHeaderOrExit(booking, dokter);
        if (antrianView is null)
            antrianView = new AntrianHeaderView("-", "", new DateOnly(3000, 1, 1), new TimeOnly(0, 0), "");

        var antrian = _antrianRepo.LoadEntity(antrianView).Value;
        var entry = antrian.ListEntry
            .FirstOrDefault(x => x.NoUrut == booking.NoAntrian);

        // AntrianMap
        var jadwal = ResolveJadwalForDelete(booking, dokter);
        var antrianMap = CekAntrianMap(jadwal, booking.TglBerobat);

        // EXECUTE
        using var trans = TransHelper.NewScope();
        _bookingRepo.DeleteEntity(booking);

        if (antrianMap.JadwalId != "-")
        {
            antrianMap.VoidSlot(booking.NoAntrian);
            _antrianMapRepo.SaveChanges(antrianMap);
        }
        if (entry is not null)
        {
            antrian.RemoveEntry(entry.NoUrut);
            _antrianRepo.SaveChanges(antrian);

            RetainTrackerWithCancellation(entry, booking.BookingId, occurredAt);
        }

        trans.Complete();
        return Task.CompletedTask;
    }

    #region Helper
    private void RetainTrackerWithCancellation(
        AntrianEntryModel entry, string bookingId, DateTime occurredAt)
    {
        var trackerId = entry.Tracker.PasienTrackerId;
        if (!PasienTrackerStableIdentity.IsRealTrackerId(trackerId))
            return;

        var trackerOpt = _pasienTrackerRepo.LoadEntity(PasienTrackerModel.Key(trackerId));
        if (!trackerOpt.HasValue)
            return;

        var tracker = trackerOpt.Value;
        tracker.AddEvent("BOOKING_CANCELLED", bookingId, occurredAt);
        _pasienTrackerRepo.SaveChanges(tracker);
    }

    private BookingModel? LoadBookingOrExit(IBookingKey key)
    {
        var opt = _bookingRepo.LoadEntity(key);
        return opt.HasValue ? opt.Value : null;
    }

    private static void EnsureBookingNotRegistered(BookingModel booking)
    {
        if (booking.Reg.RegId.Trim() != "-")
            throw new KeyNotFoundException(
                $"Booking sudah registrasi {booking.Reg.RegId}, tidak boleh delete");
    }

    private PpaType LoadDokterOrThrow(BookingModel booking)
    {
        var key = PpaType.Key(booking.Dokter.PpaId);
        return _ppaRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Dokter {key.PpaId} not found")
            );
    }

    private AntrianHeaderView? LoadAntrianHeaderOrExit(
        BookingModel booking,
        PpaType dokter)
    {
        var tag = AntrianModel.GenSequenceTag(
            booking.TglBerobat, booking.JamPraktek, dokter);

        return _antrianRepo
            .ListData(booking.TglBerobat)
            .FirstOrDefault(x => x.SequenceTag == tag);
    }

    private JadwalPraktekType ResolveJadwalForDelete(BookingModel booking, PpaType dokter)
    {
        if (_featureResolver.UseResolver)
        {
            if (!string.IsNullOrWhiteSpace(booking.JadwalPraktekHarianId))
            {
                var daily = _jadwalPraktekHarianRepo
                    .LoadEntity(JadwalPraktekHarianType.Key(booking.JadwalPraktekHarianId))
                    .GetValueOrThrow("Jadwal harian tidak ditemukan");
                return JadwalPraktekLegacyAdapter.ToTemplate(
                    JadwalPraktekEffectiveMapper.FromDaily(daily));
            }

            if (!string.IsNullOrWhiteSpace(booking.JadwalPraktekId))
            {
                return _jadwalPraktekRepo.LoadEntity(JadwalPraktekType.Key(booking.JadwalPraktekId))
                    .GetValueOrThrow("Jadwal template tidak ditemukan");
            }

            var effective = _featureResolver.Resolve(new JadwalPraktekResolveRequest(
                booking.TglBerobat, dokter, booking.JamPraktek,
                new JadwalPraktekResolveOptions(ThrowIfNotFound: true)));
            return JadwalPraktekLegacyAdapter.ToTemplate(effective);
        }

        return LegacyJadwalPraktekLookup.Resolve(
            _jadwalPraktekRepo, dokter, booking.TglBerobat, booking.JamPraktek);
    }

    private AntrianMapModel CekAntrianMap(JadwalPraktekType jadwal, DateOnly tglJadwal)
    {
        var ppaKey = PpaType.Key(jadwal.Dokter.PpaId);

        var listAntrianMap = _antrianMapRepo
            .ListData(jadwal.Layanan, ppaKey, tglJadwal)?
            .ToList() ?? [];

        var antrianThis = listAntrianMap
            .FirstOrDefault(x => x.JamJadwal == jadwal.JamMulai);

        if (antrianThis is not null)
            return _antrianMapRepo.LoadEntity(antrianThis).Value;
        else
            return AntrianMapModel.Default;
    }
    #endregion

}
