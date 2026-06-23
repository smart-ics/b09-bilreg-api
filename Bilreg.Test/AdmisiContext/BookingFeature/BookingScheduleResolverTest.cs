using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class BookingScheduleResolverTest
{
    private readonly Mock<IJadwalPraktekFeatureResolver> _featureResolver = new();
    private readonly Mock<IJadwalPraktekRepo> _legacyRepo = new();

    private static readonly DateOnly Tgl = new(2026, 7, 15);
    private static readonly PpaType Dokter = new("DR001", "Dr", "Dr", SmfType.Default,
        GroupSpesialisType.Default, [], [], []);

    [Fact]
    public void UT01_ToggleOff_UsesLegacyTemplateLookup()
    {
        var template = CreateTemplate(DayOfWeek.Wednesday, new TimeOnly(8, 0));
        _featureResolver.Setup(f => f.UseResolver).Returns(false);
        _legacyRepo.Setup(r => r.ListData(Dokter)).Returns([template]);

        var result = BookingScheduleResolver.Resolve(
            _featureResolver.Object, _legacyRepo.Object, Dokter, Tgl, new TimeOnly(8, 0));

        result.LegacyJadwal.JadwalPraktekId.Should().Be("JADW001");
        result.Effective.Source.Should().Be(JadwalPraktekSource.TEMPLATE);
        _featureResolver.Verify(f => f.Resolve(It.IsAny<JadwalPraktekResolveRequest>()), Times.Never);
    }

    [Fact]
    public void UT02_ToggleOn_UsesFeatureResolver()
    {
        var effective = JadwalPraktekEffectiveMapper.FromTemplate(
            CreateTemplate(DayOfWeek.Wednesday, new TimeOnly(8, 0)), Tgl);
        _featureResolver.Setup(f => f.UseResolver).Returns(true);
        _featureResolver.Setup(f => f.Resolve(It.IsAny<JadwalPraktekResolveRequest>()))
            .Returns(effective);

        var result = BookingScheduleResolver.Resolve(
            _featureResolver.Object, _legacyRepo.Object, Dokter, Tgl, new TimeOnly(8, 0));

        result.Effective.Source.Should().Be(JadwalPraktekSource.TEMPLATE);
        result.LegacyJadwal.Dokter.PpaId.Should().Be("DR001");
        _legacyRepo.Verify(r => r.ListData(It.IsAny<IPpaKey>()), Times.Never);
    }

    [Fact]
    public void UT03_WalkInToggleOn_AllowsSynthetic()
    {
        var effective = JadwalPraktekEffectiveMapper.Synthetic(
            Tgl, Dokter.ToReff(), new TimeOnly(10, 0), new TimeOnly(11, 0));
        _featureResolver.Setup(f => f.UseResolver).Returns(true);
        _featureResolver.Setup(f => f.Resolve(It.Is<JadwalPraktekResolveRequest>(r =>
                r.Options.AllowSynthetic)))
            .Returns(effective);

        var result = BookingScheduleResolver.ResolveWalkIn(
            _featureResolver.Object, _legacyRepo.Object, Dokter, Tgl, "10:00");

        result.Effective.Source.Should().Be(JadwalPraktekSource.SYNTHETIC);
    }

    private static JadwalPraktekType CreateTemplate(DayOfWeek hari, TimeOnly jamMulai)
        => new("JADW001", new PpaReff("DR001", "Dr"), LayananType.Default.ToReff(),
            LayananDkType.Default.ToReff(), GroupSpesialisType.Default, RuangType.Default,
            hari, jamMulai, new TimeOnly(12, 0), 30, AntrianPatternType.Default);
}
