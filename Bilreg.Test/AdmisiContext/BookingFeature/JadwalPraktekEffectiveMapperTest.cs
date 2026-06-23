using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class JadwalPraktekEffectiveMapperTest
{
    [Fact]
    public void UT01_FromTemplate_SetsSourceTemplateAndNullHarianId()
    {
        var template = CreateTemplate(DayOfWeek.Monday, new TimeOnly(8, 0));
        var tgl = new DateOnly(2026, 7, 13);

        var result = JadwalPraktekEffectiveMapper.FromTemplate(template, tgl);

        result.JadwalPraktekId.Should().Be(template.JadwalPraktekId);
        result.JadwalPraktekHarianId.Should().BeNull();
        result.TglPraktek.Should().Be(tgl);
        result.Source.Should().Be(JadwalPraktekSource.TEMPLATE);
        result.Status.Should().Be(JadwalPraktekScheduleStatus.ACTIVE);
    }

    [Fact]
    public void UT02_FromDaily_ManualSource_MapsToDailyManual()
    {
        var daily = CreateDaily(JadwalPraktekHarianSource.MANUAL, JadwalPraktekScheduleStatus.ACTIVE);

        var result = JadwalPraktekEffectiveMapper.FromDaily(daily);

        result.Source.Should().Be(JadwalPraktekSource.DAILY_MANUAL);
        result.JadwalPraktekHarianId.Should().Be(daily.JadwalPraktekHarianId);
    }

    [Fact]
    public void UT03_FromDaily_GeneratedSource_MapsToDailyGenerated()
    {
        var daily = CreateDaily(JadwalPraktekHarianSource.GENERATED, JadwalPraktekScheduleStatus.ACTIVE);

        var result = JadwalPraktekEffectiveMapper.FromDaily(daily);

        result.Source.Should().Be(JadwalPraktekSource.DAILY_GENERATED);
    }

    [Fact]
    public void UT04_Synthetic_HasNullIdsAndSyntheticSource()
    {
        var dokter = new PpaReff("DR001", "Dr. Test");
        var tgl = new DateOnly(2026, 7, 15);

        var result = JadwalPraktekEffectiveMapper.Synthetic(
            tgl, dokter, new TimeOnly(0, 0), new TimeOnly(23, 59));

        result.JadwalPraktekId.Should().BeNull();
        result.JadwalPraktekHarianId.Should().BeNull();
        result.Source.Should().Be(JadwalPraktekSource.SYNTHETIC);
        result.Dokter.PpaId.Should().Be("DR001");
    }

    private static JadwalPraktekType CreateTemplate(DayOfWeek hari, TimeOnly jamMulai)
    {
        var dokter = new PpaReff("DR001", "Dr. Test");
        return new JadwalPraktekType(
            "JADW001", dokter,
            new LayananReff("LY001", "Poli"),
            new LayananDkReff("1", "UMUM"),
            GroupSpesialisType.Default,
            new RuangType("RU01", "RUANG1", "A"),
            hari, jamMulai, new TimeOnly(12, 0), 30,
            AntrianPatternType.Default);
    }

    private static JadwalPraktekHarianType CreateDaily(
        JadwalPraktekHarianSource source,
        JadwalPraktekScheduleStatus status)
    {
        var dokter = new PpaReff("DR001", "Dr. Test");
        return new JadwalPraktekHarianType(
            "JPH00000001", "JADW001",
            new DateOnly(2026, 7, 15),
            dokter,
            new LayananReff("LY001", "Poli"),
            new LayananDkReff("1", "UMUM"),
            GroupSpesialisType.Default,
            new RuangType("RU01", "RUANG1", "A"),
            new TimeOnly(8, 0), new TimeOnly(12, 0), 30,
            AntrianPatternType.Default,
            status, source, null,
            AuditTrailType.Default);
    }
}
