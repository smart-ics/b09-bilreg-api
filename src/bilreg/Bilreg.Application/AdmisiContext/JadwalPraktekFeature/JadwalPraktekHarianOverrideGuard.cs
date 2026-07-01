using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature;

public interface IJadwalPraktekHarianOverrideGuard
{
    void EnsureNoOperationalConflict(DateOnly tglPraktek, IPpaKey dokter, TimeOnly jamMulai);
}

public class JadwalPraktekHarianOverrideGuard : IJadwalPraktekHarianOverrideGuard
{
    private readonly IBookingRepo _bookingRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IPpaRepo _ppaRepo;

    public JadwalPraktekHarianOverrideGuard(
        IBookingRepo bookingRepo,
        IAntrianRepo antrianRepo,
        IPpaRepo ppaRepo)
    {
        _bookingRepo = bookingRepo;
        _antrianRepo = antrianRepo;
        _ppaRepo = ppaRepo;
    }

    public void EnsureNoOperationalConflict(DateOnly tglPraktek, IPpaKey dokter, TimeOnly jamMulai)
    {
        var periode = new Periode(tglPraktek.ToDateTime(TimeOnly.MinValue));
        var hasBooking = (_bookingRepo.ListDataTglBerobat(periode) ?? [])
            .Any(b => b.Dokter.PpaId == dokter.PpaId
                      && b.JamPraktek == jamMulai
                      && b.TglBerobat == tglPraktek);

        if (hasBooking)
            throw new InvalidOperationException(
                "Tidak dapat mengubah jadwal harian karena masih ada booking aktif pada sesi ini");

        var ppa = _ppaRepo.LoadEntity(dokter)
            .GetValueOrThrow("Dokter tidak ditemukan");
        var sequenceTag = AntrianModel.GenSequenceTag(tglPraktek, jamMulai, ppa);
        var antrianHeaders = _antrianRepo.ListData(tglPraktek) ?? [];
        var hasAntrian = antrianHeaders.Any(x => x.SequenceTag == sequenceTag);

        if (hasAntrian)
            throw new InvalidOperationException(
                "Tidak dapat mengubah jadwal harian karena masih ada antrian aktif pada sesi ini");
    }
}
