using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class QueListAntrianHeaderQueryTest
{
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IJadwalPraktekRepo> _jadwalRepo = new();
    private readonly Mock<IJadwalPraktekFeatureResolver> _featureResolver = new();

    private static readonly DateOnly TargetDate = new(2026, 7, 15); // Wednesday
    private const string TargetDateStr = "2026-07-15";

    private QueListAntrianHeaderHandler CreateSut() =>
        new(_antrianRepo.Object, _jadwalRepo.Object, _featureResolver.Object);

    [Fact]
    public async Task Handle_WhenResolverEnabled_MapsRuangFromResolvedSchedule()
    {
        // Arrange
        _featureResolver.Setup(f => f.UseResolver).Returns(true);

        var antrianHeader = new AntrianHeaderView(
            AntrianId: "ANT01",
            AntrianDescription: "Antrian Poli Anak",
            AntrianDate: TargetDate,
            StartTime: new TimeOnly(8, 0),
            SequenceTag: "AN2607150800_DR001"
        );
        _antrianRepo.Setup(r => r.ListData(TargetDate))
            .Returns([antrianHeader]);

        var effective = new JadwalPraktekEffective
        {
            JadwalPraktekId = "JADW001",
            TglPraktek = TargetDate,
            Dokter = new PpaReff("DR001", "dr. Budi, Sp.A"),
            Layanan = new LayananReff("POLI01", "Poli Anak"),
            Ruang = new RuangType("R01", "Klinik Anak 1", "A"),
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0),
            MaxPasien = 20
        };
        _featureResolver.Setup(f => f.ResolveForDate(It.Is<JadwalPraktekResolveForDateRequest>(r => r.TglPraktek == TargetDate)))
            .Returns([effective]);

        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(new QueListAntrianHeaderQuery(TargetDateStr), CancellationToken.None)).ToList();

        // Assert
        result.Should().ContainSingle();
        var item = result[0];
        item.AntrianId.Should().Be("ANT01");
        item.Dokter.PpaId.Should().Be("DR001");
        item.Dokter.PpaName.Should().Be("dr. Budi, Sp.A");
        item.Layanan.LayananId.Should().Be("POLI01");
        item.JamMulai.Should().Be("08:00");
        item.Ruang.Should().NotBeNull();
        item.Ruang.RuangId.Should().Be("R01");
        item.Ruang.RuangName.Should().Be("Klinik Anak 1");
        item.Ruang.PrefixAntrian.Should().Be("A");
    }

    [Fact]
    public async Task Handle_WhenResolverDisabled_MapsRuangFromLegacyJadwalRepo()
    {
        // Arrange
        _featureResolver.Setup(f => f.UseResolver).Returns(false);

        var antrianHeader = new AntrianHeaderView(
            AntrianId: "ANT02",
            AntrianDescription: "Antrian Poli Bedah",
            AntrianDate: TargetDate,
            StartTime: new TimeOnly(9, 0),
            SequenceTag: "AN2607150900_DR002"
        );
        _antrianRepo.Setup(r => r.ListData(TargetDate))
            .Returns([antrianHeader]);

        var jadwal = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "JADW002",
            Dokter = new PpaReff("DR002", "dr. Siti, Sp.B"),
            Layanan = new LayananReff("POLI02", "Poli Bedah"),
            Ruang = new RuangType("R02", "Klinik Bedah 1", "B"),
            Hari = TargetDate.DayOfWeek,
            JamMulai = new TimeOnly(9, 0),
            JamSelesai = new TimeOnly(13, 0)
        };
        _jadwalRepo.Setup(r => r.ListData())
            .Returns([jadwal]);

        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(new QueListAntrianHeaderQuery(TargetDateStr), CancellationToken.None)).ToList();

        // Assert
        result.Should().ContainSingle();
        var item = result[0];
        item.AntrianId.Should().Be("ANT02");
        item.Dokter.PpaId.Should().Be("DR002");
        item.Ruang.Should().NotBeNull();
        item.Ruang.RuangId.Should().Be("R02");
        item.Ruang.RuangName.Should().Be("Klinik Bedah 1");
        item.Ruang.PrefixAntrian.Should().Be("B");
    }

    [Fact]
    public async Task Handle_WhenScheduleRuangIsNull_FallsBackToDefaultRuang()
    {
        // Arrange
        _featureResolver.Setup(f => f.UseResolver).Returns(false);

        var antrianHeader = new AntrianHeaderView(
            AntrianId: "ANT03",
            AntrianDescription: "Antrian Poli Umum",
            AntrianDate: TargetDate,
            StartTime: new TimeOnly(10, 0),
            SequenceTag: "AN2607151000_DR003"
        );
        _antrianRepo.Setup(r => r.ListData(TargetDate))
            .Returns([antrianHeader]);

        var jadwal = JadwalPraktekType.Default with
        {
            Dokter = new PpaReff("DR003", "dr. Ahmad"),
            Hari = TargetDate.DayOfWeek,
            JamMulai = new TimeOnly(10, 0),
            Ruang = null!
        };
        _jadwalRepo.Setup(r => r.ListData())
            .Returns([jadwal]);

        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(new QueListAntrianHeaderQuery(TargetDateStr), CancellationToken.None)).ToList();

        // Assert
        result.Should().ContainSingle();
        var item = result[0];
        item.Ruang.Should().NotBeNull();
        item.Ruang.Should().Be(RuangType.Default);
        item.Ruang.RuangId.Should().Be("-");
        item.Ruang.RuangName.Should().Be("-");
        item.Ruang.PrefixAntrian.Should().Be("-");
    }

    [Fact]
    public async Task Handle_WhenDateFormatInvalid_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(new QueListAntrianHeaderQuery("invalid-date"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
