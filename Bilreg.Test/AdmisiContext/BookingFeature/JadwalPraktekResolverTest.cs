using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class JadwalPraktekResolverTest
{
    private readonly Mock<IJadwalPraktekRepo> _templateRepo = new();
    private readonly Mock<IJadwalPraktekHarianRepo> _dailyRepo = new();
    private readonly JadwalPraktekResolver _sut;

    private static readonly DateOnly Tgl = new(2026, 7, 15); // Wednesday
    private static readonly IPpaKey Dokter = PpaType.Key("DR001");

    public JadwalPraktekResolverTest()
    {
        _sut = new JadwalPraktekResolver(_templateRepo.Object, _dailyRepo.Object);
    }

    [Fact]
    public void UT01_Resolve_FromTemplate_WhenNoDailyRow()
    {
        var template = CreateTemplate(DayOfWeek.Wednesday, new TimeOnly(8, 0));
        _dailyRepo.Setup(r => r.ListByDateAndDokter(Tgl, Dokter)).Returns([]);
        _templateRepo.Setup(r => r.ListData(Dokter)).Returns([template]);

        var result = _sut.Resolve(Req(jamMulai: new TimeOnly(8, 0)));

        result.Source.Should().Be(JadwalPraktekSource.TEMPLATE);
        result.JadwalPraktekId.Should().Be("JADW001");
        result.TglPraktek.Should().Be(Tgl);
    }

    [Fact]
    public void UT02_Resolve_ManualDailyWinsOverTemplate()
    {
        var template = CreateTemplate(DayOfWeek.Wednesday, new TimeOnly(8, 0));
        var daily = CreateDaily(new TimeOnly(9, 0), JadwalPraktekHarianSource.MANUAL);
        _dailyRepo.Setup(r => r.ListByDateAndDokter(Tgl, Dokter)).Returns([daily]);
        _templateRepo.Setup(r => r.ListData(Dokter)).Returns([template]);

        var result = _sut.Resolve(Req(jamMulai: new TimeOnly(9, 0)));

        result.Source.Should().Be(JadwalPraktekSource.DAILY_MANUAL);
        result.JamMulai.Should().Be(new TimeOnly(9, 0));
    }

    [Fact]
    public void UT03_Resolve_CancelledDaily_ThrowsWhenThrowIfCancelled()
    {
        var daily = CreateDaily(new TimeOnly(8, 0), JadwalPraktekHarianSource.MANUAL,
            JadwalPraktekScheduleStatus.CANCELLED);
        _dailyRepo.Setup(r => r.ListByDateAndDokter(Tgl, Dokter)).Returns([daily]);

        var act = () => _sut.Resolve(Req(jamMulai: new TimeOnly(8, 0)));

        act.Should().Throw<ArgumentException>().WithMessage("*dibatalkan*");
    }

    [Fact]
    public void UT04_Resolve_Synthetic_WhenAllowSyntheticAndNoSchedule()
    {
        _dailyRepo.Setup(r => r.ListByDateAndDokter(Tgl, Dokter)).Returns([]);
        _templateRepo.Setup(r => r.ListData(Dokter)).Returns([]);

        var result = _sut.Resolve(Req(
            jamMulai: new TimeOnly(10, 0),
            options: new JadwalPraktekResolveOptions(AllowSynthetic: true)));

        result.Source.Should().Be(JadwalPraktekSource.SYNTHETIC);
    }

    [Fact]
    public void UT05_Resolve_MultiSessionWithoutJamMulai_Throws()
    {
        var t1 = CreateTemplate(DayOfWeek.Wednesday, new TimeOnly(8, 0));
        var t2 = CreateTemplate(DayOfWeek.Wednesday, new TimeOnly(13, 0));
        _dailyRepo.Setup(r => r.ListByDateAndDokter(Tgl, Dokter)).Returns([]);
        _templateRepo.Setup(r => r.ListData(Dokter)).Returns([t1, t2]);

        var act = () => _sut.Resolve(Req(jamMulai: null));

        act.Should().Throw<ArgumentException>().WithMessage("*JamMulai wajib*");
    }

    [Fact]
    public void UT06_Resolve_MultiSessionWithJamMulai_ReturnsMatchingSession()
    {
        var t1 = CreateTemplate(DayOfWeek.Wednesday, new TimeOnly(8, 0));
        var t2 = CreateTemplate(DayOfWeek.Wednesday, new TimeOnly(13, 0));
        _dailyRepo.Setup(r => r.ListByDateAndDokter(Tgl, Dokter)).Returns([]);
        _templateRepo.Setup(r => r.ListData(Dokter)).Returns([t1, t2]);

        var result = _sut.Resolve(Req(jamMulai: new TimeOnly(13, 0)));

        result.JamMulai.Should().Be(new TimeOnly(13, 0));
    }

    [Fact]
    public void UT07_ResolveForDate_DeduplicatesDailyOverTemplate()
    {
        var template = CreateTemplate(DayOfWeek.Wednesday, new TimeOnly(8, 0));
        var daily = CreateDaily(new TimeOnly(8, 0), JadwalPraktekHarianSource.MANUAL);
        _dailyRepo.Setup(r => r.ListByDate(Tgl)).Returns([daily]);
        _templateRepo.Setup(r => r.ListData()).Returns([template]);

        var results = _sut.ResolveForDate(new JadwalPraktekResolveForDateRequest(Tgl)).ToList();

        results.Should().HaveCount(1);
        results[0].Source.Should().Be(JadwalPraktekSource.DAILY_MANUAL);
    }

    [Fact]
    public void UT08_ResolveForDate_ExcludesCancelledByDefault()
    {
        var cancelled = CreateDaily(new TimeOnly(8, 0), JadwalPraktekHarianSource.MANUAL,
            JadwalPraktekScheduleStatus.CANCELLED);
        _dailyRepo.Setup(r => r.ListByDate(Tgl)).Returns([cancelled]);
        _templateRepo.Setup(r => r.ListData()).Returns([]);

        var results = _sut.ResolveForDate(new JadwalPraktekResolveForDateRequest(Tgl)).ToList();

        results.Should().BeEmpty();
    }

    [Fact]
    public void UT09_ResolveForDate_IncludesCancelledWhenRequested()
    {
        var cancelled = CreateDaily(new TimeOnly(8, 0), JadwalPraktekHarianSource.MANUAL,
            JadwalPraktekScheduleStatus.CANCELLED);
        _dailyRepo.Setup(r => r.ListByDate(Tgl)).Returns([cancelled]);
        _templateRepo.Setup(r => r.ListData()).Returns([]);

        var results = _sut.ResolveForDate(
            new JadwalPraktekResolveForDateRequest(Tgl, IncludeCancelled: true)).ToList();

        results.Should().HaveCount(1);
        results[0].Status.Should().Be(JadwalPraktekScheduleStatus.CANCELLED);
    }

    [Fact]
    public void UT10_Resolve_ReplacementDoctor_UsesDailyDokterId()
    {
        var daily = new JadwalPraktekHarianType(
            "JPH00000002", "JADW001", Tgl,
            new PpaReff("DR002", "Dr. Substitute"),
            new LayananReff("LY001", "Poli"),
            new LayananDkReff("1", "UMUM"),
            GroupSpesialisType.Default,
            new RuangType("RU01", "RUANG1", "A"),
            new TimeOnly(8, 0), new TimeOnly(12, 0), 30,
            AntrianPatternType.Default,
            JadwalPraktekScheduleStatus.ACTIVE,
            JadwalPraktekHarianSource.MANUAL, "Substitute",
            AuditTrailType.Default);
        _dailyRepo.Setup(r => r.ListByDateAndDokter(Tgl, It.Is<IPpaKey>(k => k.PpaId == "DR002")))
            .Returns([daily]);
        _templateRepo.Setup(r => r.ListData(It.Is<IPpaKey>(k => k.PpaId == "DR002"))).Returns([]);

        var result = _sut.Resolve(new JadwalPraktekResolveRequest(
            Tgl, PpaType.Key("DR002"), new TimeOnly(8, 0),
            new JadwalPraktekResolveOptions()));

        result.Dokter.PpaId.Should().Be("DR002");
        result.Source.Should().Be(JadwalPraktekSource.DAILY_MANUAL);
    }

    private static JadwalPraktekResolveRequest Req(
        TimeOnly? jamMulai = null,
        JadwalPraktekResolveOptions? options = null)
        => new(Tgl, Dokter, jamMulai, options ?? new JadwalPraktekResolveOptions());

    private static JadwalPraktekType CreateTemplate(DayOfWeek hari, TimeOnly jamMulai)
        => new(
            "JADW001", new PpaReff("DR001", "Dr. Test"),
            new LayananReff("LY001", "Poli"),
            new LayananDkReff("1", "UMUM"),
            GroupSpesialisType.Default,
            new RuangType("RU01", "RUANG1", "A"),
            hari, jamMulai, new TimeOnly(12, 0), 30,
            AntrianPatternType.Default);

    private static JadwalPraktekHarianType CreateDaily(
        TimeOnly jamMulai,
        JadwalPraktekHarianSource source,
        JadwalPraktekScheduleStatus status = JadwalPraktekScheduleStatus.ACTIVE)
        => new(
            "JPH00000001", "JADW001", Tgl,
            new PpaReff("DR001", "Dr. Test"),
            new LayananReff("LY001", "Poli"),
            new LayananDkReff("1", "UMUM"),
            GroupSpesialisType.Default,
            new RuangType("RU01", "RUANG1", "A"),
            jamMulai, new TimeOnly(12, 0), 30,
            AntrianPatternType.Default,
            status, source, null,
            AuditTrailType.Default);
}
