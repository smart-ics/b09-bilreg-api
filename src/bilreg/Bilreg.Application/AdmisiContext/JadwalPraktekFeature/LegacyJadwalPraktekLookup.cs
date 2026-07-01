using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using System.Globalization;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature;

/// <summary>
/// Legacy inline schedule lookup (toggle OFF path). Extracted to avoid a third resolution implementation.
/// </summary>
public static class LegacyJadwalPraktekLookup
{
    public static JadwalPraktekType Resolve(
        IJadwalPraktekRepo jadwalPraktekRepo,
        IPpaKey dokter,
        DateOnly tglBerobat,
        TimeOnly jamMulai)
    {
        var listJadwal = jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var hari = tglBerobat.DayOfWeek;
        return listJadwal
            .Where(x => x.Hari == hari)
            .FirstOrDefault(x => x.JamMulai == jamMulai)
            ?? throw new ArgumentException("Jadwal tidak ditemukan");
    }

    public static JadwalPraktekType ResolveWithSyntheticFallback(
        IJadwalPraktekRepo jadwalPraktekRepo,
        PpaType dokter,
        DateOnly tglBerobat,
        string jamPraktek)
    {
        var listJadwal = jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var listJadwalHari = listJadwal.Where(x => x.Hari == tglBerobat.DayOfWeek).ToList();

        return listJadwalHari.Count switch
        {
            1 => listJadwalHari.First(),
            > 1 => listJadwalHari.FirstOrDefault(x =>
                x.JamMulai == TimeOnly.ParseExact(jamPraktek, "HH:mm", CultureInfo.InvariantCulture))
                ?? throw new ArgumentException($"Dokter tidak praktek pada jam {jamPraktek}"),
            _ => JadwalPraktekType.Default with
            {
                Dokter = dokter.ToReff(),
                Hari = tglBerobat.DayOfWeek,
                JamMulai = TimeOnly.ParseExact("00:00", "HH:mm", CultureInfo.InvariantCulture),
                JamSelesai = TimeOnly.ParseExact("23:59", "HH:mm", CultureInfo.InvariantCulture)
            }
        };
    }
}
