using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;

public static class JadwalPraktekEffectiveMapper
{
    public static JadwalPraktekEffective FromTemplate(JadwalPraktekType template, DateOnly tglPraktek)
        => new()
        {
            JadwalPraktekId = template.JadwalPraktekId,
            JadwalPraktekHarianId = null,
            TglPraktek = tglPraktek,
            Dokter = template.Dokter,
            Layanan = template.Layanan,
            LayananDk = template.LayananDk,
            GroupSpesialis = template.GroupSpesialis,
            Ruang = template.Ruang,
            JamMulai = template.JamMulai,
            JamSelesai = template.JamSelesai,
            MaxPasien = template.MaxPasien,
            AntrianPattern = template.AntrianPattern,
            Status = JadwalPraktekScheduleStatus.ACTIVE,
            Source = JadwalPraktekSource.TEMPLATE
        };

    public static JadwalPraktekEffective FromDaily(JadwalPraktekHarianType daily)
    {
        var source = daily.Source == JadwalPraktekHarianSource.MANUAL
            ? JadwalPraktekSource.DAILY_MANUAL
            : JadwalPraktekSource.DAILY_GENERATED;

        return new JadwalPraktekEffective
        {
            JadwalPraktekId = daily.JadwalPraktekId,
            JadwalPraktekHarianId = daily.JadwalPraktekHarianId,
            TglPraktek = daily.TglPraktek,
            Dokter = daily.Dokter,
            Layanan = daily.Layanan,
            LayananDk = daily.LayananDk,
            GroupSpesialis = daily.GroupSpesialis,
            Ruang = daily.Ruang,
            JamMulai = daily.JamMulai,
            JamSelesai = daily.JamSelesai,
            MaxPasien = daily.MaxPasien,
            AntrianPattern = daily.AntrianPattern,
            Status = daily.Status,
            Source = source
        };
    }

    public static JadwalPraktekEffective Synthetic(
        DateOnly tglPraktek, PpaReff dokter, TimeOnly jamMulai, TimeOnly jamSelesai)
        => new()
        {
            JadwalPraktekId = null,
            JadwalPraktekHarianId = null,
            TglPraktek = tglPraktek,
            Dokter = dokter,
            Layanan = LayananType.Default.ToReff(),
            LayananDk = LayananDkType.Default.ToReff(),
            GroupSpesialis = GroupSpesialisType.Default,
            Ruang = RuangType.Default,
            JamMulai = jamMulai,
            JamSelesai = jamSelesai,
            MaxPasien = int.MaxValue,
            AntrianPattern = AntrianPatternType.Default,
            Status = JadwalPraktekScheduleStatus.ACTIVE,
            Source = JadwalPraktekSource.SYNTHETIC
        };
}
