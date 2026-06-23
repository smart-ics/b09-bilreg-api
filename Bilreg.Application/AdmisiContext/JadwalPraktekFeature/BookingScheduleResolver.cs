using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature;

public record BookingScheduleContext(
    JadwalPraktekEffective Effective,
    JadwalPraktekType LegacyJadwal);

public static class BookingScheduleResolver
{
    public static BookingScheduleContext Resolve(
        IJadwalPraktekFeatureResolver featureResolver,
        IJadwalPraktekRepo legacyRepo,
        IPpaKey dokter,
        DateOnly tglBerobat,
        TimeOnly jamMulai,
        bool allowSynthetic = false)
    {
        if (featureResolver.UseResolver)
        {
            var effective = featureResolver.Resolve(new JadwalPraktekResolveRequest(
                tglBerobat, dokter, jamMulai,
                new JadwalPraktekResolveOptions(AllowSynthetic: allowSynthetic)));
            return new BookingScheduleContext(
                effective, JadwalPraktekLegacyAdapter.ToTemplate(effective));
        }

        var jadwal = LegacyJadwalPraktekLookup.Resolve(legacyRepo, dokter, tglBerobat, jamMulai);
        var fromTemplate = JadwalPraktekEffectiveMapper.FromTemplate(jadwal, tglBerobat);
        return new BookingScheduleContext(fromTemplate, jadwal);
    }

    public static BookingScheduleContext ResolveWalkIn(
        IJadwalPraktekFeatureResolver featureResolver,
        IJadwalPraktekRepo legacyRepo,
        PpaType dokter,
        DateOnly tglBerobat,
        string jamPraktek)
    {
        if (featureResolver.UseResolver)
        {
            var jam = TimeOnly.Parse(jamPraktek);
            var effective = featureResolver.Resolve(new JadwalPraktekResolveRequest(
                tglBerobat, dokter, jam,
                new JadwalPraktekResolveOptions(AllowSynthetic: true)));
            return new BookingScheduleContext(
                effective, JadwalPraktekLegacyAdapter.ToTemplate(effective));
        }

        var jadwal = LegacyJadwalPraktekLookup.ResolveWithSyntheticFallback(
            legacyRepo, dokter, tglBerobat, jamPraktek);
        var effectiveLegacy = jadwal.JadwalPraktekId == "-"
            ? JadwalPraktekEffectiveMapper.Synthetic(
                tglBerobat, dokter.ToReff(), jadwal.JamMulai, jadwal.JamSelesai)
            : JadwalPraktekEffectiveMapper.FromTemplate(jadwal, tglBerobat);
        return new BookingScheduleContext(effectiveLegacy, jadwal);
    }
}
