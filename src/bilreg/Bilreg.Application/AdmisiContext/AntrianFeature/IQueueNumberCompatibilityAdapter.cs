using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IQueueNumberCompatibilityAdapter
{
    Result<ReservedQueueNumber> ReserveForBooking(
        JadwalPraktekType jadwal,
        DateOnly tgl,
        BookingModel booking,
        PasienModel pasien);

    Result<ReservedQueueNumber> ReserveForRegistration(
        JadwalPraktekType jadwal,
        DateOnly tgl,
        RegModel reg);

    AntrianEntryModel ProjectIntoQueueSession(
        AntrianModel antrian,
        ReservedQueueNumber reserved,
        PasienTrackerModel tracker,
        string reffId,
        string reffDesc,
        DateTime createdAt);

    AntrianEntryModel AcceptExternalNumber(
        AntrianModel antrian,
        int noUrut,
        PasienTrackerModel tracker,
        string reffId,
        string reffDesc,
        DateTime createdAt);

    void Release(AntrianMapModel map, int noUrut);

    bool ProjectSourceReffForRegistration(AntrianMapModel map, int noUrut, RegModel reg);
}
