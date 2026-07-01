using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature;

/// <summary>
/// Converts runtime <see cref="JadwalPraktekEffective"/> back to <see cref="JadwalPraktekType"/>
/// for legacy application APIs that have not yet been migrated off the template model.
/// </summary>
public static class JadwalPraktekLegacyAdapter
{
    public static JadwalPraktekType ToTemplate(JadwalPraktekEffective effective)
        => new(
            effective.JadwalPraktekId ?? "-",
            effective.Dokter,
            effective.Layanan,
            effective.LayananDk,
            effective.GroupSpesialis,
            effective.Ruang,
            effective.TglPraktek.DayOfWeek,
            effective.JamMulai,
            effective.JamSelesai,
            effective.MaxPasien,
            effective.AntrianPattern);
}
