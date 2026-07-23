using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>Loads legacy physician AntrianMap headers for compatibility projection.</summary>
public static class PhysicianAntrianMapLookup
{
    public static AntrianMapModel FindForBooking(IAntrianMapRepo repo, BookingModel booking)
    {
        var list = repo.ListData(
            LayananType.Key(booking.Layanan.LayananId),
            PpaType.Key(booking.Dokter.PpaId),
            booking.TglBerobat)?.ToList() ?? [];

        var hdr = list.FirstOrDefault(x => x.JamJadwal == booking.JamPraktek);
        return hdr is null
            ? AntrianMapModel.Default
            : repo.LoadEntity(AntrianMapModel.Key(hdr.AntrianMapId)).Value;
    }

    public static AntrianMapModel FindForRegQueue(IAntrianMapRepo repo, RegModel reg, AntrianModel queue)
    {
        var list = repo.ListData(
            LayananType.Key(reg.Layanan.LayananId),
            PpaType.Key(reg.Dokter.PpaId),
            reg.RegDate)?.ToList() ?? [];

        var hdr = list.FirstOrDefault(x => x.JamJadwal == queue.StartTime);
        return hdr is null
            ? AntrianMapModel.Default
            : repo.LoadEntity(AntrianMapModel.Key(hdr.AntrianMapId)).Value;
    }
}
