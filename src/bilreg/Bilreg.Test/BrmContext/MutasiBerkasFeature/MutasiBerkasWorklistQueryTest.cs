using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasWorklistQueryTest
{
    private readonly Mock<IMutasiBerkasWorklistDal> _worklistDalMock = new();
    private readonly Mock<ILokasiBerkasDal> _lokasiDalMock = new();

    private MutasiBerkasWorklistHandler CreateWorklistSut() =>
        new(_worklistDalMock.Object);

    private LokasiBerkasF12Handler CreateLokasiSut() =>
        new(_lokasiDalMock.Object);

    [Fact]
    public async Task Handle_WorklistWithAllFilters_PassesFilterToDalAndReturnsResults()
    {
        // Arrange
        var query = new MutasiBerkasWorklistQuery(
            TglPraktek: "2026-10-17",
            LayananId: "POL01",
            SesiPoli: "1",
            Status: 1,
            GedungLantai: "G1L2"
        );

        var expectedItems = new List<MutasiBerkasWorklistItemDto>
        {
            new()
            {
                MrId = "00-12-34-56",
                PasienName = "Budi Santoso",
                AlamatPasien = "Jl. Merdeka No. 10",
                TglLahir = "1990-05-15",
                JenisKelamin = "L",
                TeleponPasien = "08123456789",
                MutasiId = "MK26100001",
                TrsBuatId = "RG2610170001",
                RegId = "RG2610170001",
                TglMutasi = "2026-10-17",
                JamMutasi = "08:00:00",
                DokterId = "DR001",
                DokterName = "dr. Andi",
                LayananId = "POL01",
                LayananName = "Poli Penyakit Dalam",
                TglPraktek = "2026-10-17",
                JamPraktek = "08:00",
                SesiPoliId = "1",
                SesiPoliName = "Pagi",
                Status = 1,
                FrekuensiCetak = 1,
                NoAntrian = 5,
                LokasiBerkasId = "LK01",
                LokasiBerkasName = "Rak Filing A"
            }
        };

        MutasiBerkasWorklistFilter? capturedFilter = null;
        _worklistDalMock
            .Setup(x => x.QueryWorklist(It.IsAny<MutasiBerkasWorklistFilter>()))
            .Callback<MutasiBerkasWorklistFilter>(f => capturedFilter = f)
            .Returns(expectedItems);

        var sut = CreateWorklistSut();

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].MrId.Should().Be("00-12-34-56");
        result[0].PasienName.Should().Be("Budi Santoso");
        result[0].MutasiId.Should().Be("MK26100001");
        result[0].FrekuensiCetak.Should().Be(1);
        result[0].NoAntrian.Should().Be(5);
        result[0].LokasiBerkasName.Should().Be("Rak Filing A");

        capturedFilter.Should().NotBeNull();
        capturedFilter!.TglPraktek.Should().Be("2026-10-17");
        capturedFilter.LayananId.Should().Be("POL01");
        capturedFilter.SesiPoli.Should().Be("1");
        capturedFilter.Status.Should().Be(1);
        capturedFilter.GedungLantai.Should().Be("G1L2");
    }

    [Fact]
    public async Task Handle_WorklistWithNoFilters_PassesDefaultFilterAndReturnsResults()
    {
        // Arrange
        var query = new MutasiBerkasWorklistQuery();

        var expectedItems = new List<MutasiBerkasWorklistItemDto>
        {
            new() { MrId = "00-12-34-56", MutasiId = "MK01", Status = 0 },
            new() { MrId = "00-12-34-57", MutasiId = "MK02", Status = 1 }
        };

        MutasiBerkasWorklistFilter? capturedFilter = null;
        _worklistDalMock
            .Setup(x => x.QueryWorklist(It.IsAny<MutasiBerkasWorklistFilter>()))
            .Callback<MutasiBerkasWorklistFilter>(f => capturedFilter = f)
            .Returns(expectedItems);

        var sut = CreateWorklistSut();

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        capturedFilter.Should().NotBeNull();
        capturedFilter!.TglPraktek.Should().BeNull();
        capturedFilter.LayananId.Should().BeNull();
        capturedFilter.Status.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WorklistEmptyResults_ReturnsEmptyCollection()
    {
        // Arrange
        var query = new MutasiBerkasWorklistQuery(LayananId: "POL99");
        _worklistDalMock
            .Setup(x => x.QueryWorklist(It.IsAny<MutasiBerkasWorklistFilter>()))
            .Returns(Enumerable.Empty<MutasiBerkasWorklistItemDto>());

        var sut = CreateWorklistSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LokasiBerkasF12Query_ReturnsAllLocationsFromDal()
    {
        // Arrange
        var expectedLocations = new List<LokasiBerkasDto>
        {
            LokasiBerkasDto.Create("LK01", "Rak Filing A", "POL01"),
            LokasiBerkasDto.Create("LK02", "Rak Filing B", "POL02")
        };

        _lokasiDalMock
            .Setup(x => x.ListAll())
            .Returns(expectedLocations);

        var sut = CreateLokasiSut();

        // Act
        var result = (await sut.Handle(new LokasiBerkasF12Query(), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].FS_KD_LOKASI_BERKAS.Should().Be("LK01");
        result[0].LokasiBerkasName.Should().Be("Rak Filing A");
        result[1].FS_KD_LOKASI_BERKAS.Should().Be("LK02");
        result[1].LokasiBerkasName.Should().Be("Rak Filing B");
        _lokasiDalMock.Verify(x => x.ListAll(), Times.Once);
    }

    [Fact]
    public async Task Handle_LokasiBerkasF12Query_WhenDalReturnsEmpty_ReturnsEmptyCollection()
    {
        // Arrange
        _lokasiDalMock
            .Setup(x => x.ListAll())
            .Returns(Enumerable.Empty<LokasiBerkasDto>());

        var sut = CreateLokasiSut();

        // Act
        var result = await sut.Handle(new LokasiBerkasF12Query(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
