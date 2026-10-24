using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TrfListTarifRajalHandlerTest
{
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepoMock = new();
    private readonly TrfListTarifRajalHandler _sut;

    public TrfListTarifRajalHandlerTest()
    {
        _sut = new TrfListTarifRajalHandler(_nilaiTarifRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ReturnsMappedTarifsOrderedByName()
    {
        // Arrange
        var query = new TrfListTarifRajalQuery("POLI-01", "KLS-01", "TT-01", "tindakan");

        var searchResult = new List<NilaiTarifView>
        {
            new("TRF-02", "Pemeriksaan USG", new TipeTarifReff("TT-01", "Umum"), new KelasReff("KLS-01", "Kelas 1"), 250000m),
            new("TRF-01", "Konsultasi Dokter Spesialis", new TipeTarifReff("TT-01", "Umum"), new KelasReff("KLS-01", "Kelas 1"), 150000m),
            new("TRF-03", "Biopsi Jaringan", new TipeTarifReff("TT-01", "Umum"), new KelasReff("KLS-01", "Kelas 1"), 500000m)
        };

        _nilaiTarifRepoMock
            .Setup(x => x.Search(
                It.Is<ILayananKey>(l => l.LayananId == "POLI-01"),
                It.Is<INilaiTarifVariant>(v => v.KelasId == "KLS-01" && v.TipeTarifId == "TT-01"),
                "tindakan"))
            .Returns(searchResult);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var list = result.ToList();
        list.Should().HaveCount(3);
        list[0].TarifId.Should().Be("TRF-03");
        list[0].TarifName.Should().Be("Biopsi Jaringan");
        list[0].Harga.Should().Be(500000m);

        list[1].TarifId.Should().Be("TRF-01");
        list[1].TarifName.Should().Be("Konsultasi Dokter Spesialis");
        list[1].Harga.Should().Be(150000m);

        list[2].TarifId.Should().Be("TRF-02");
        list[2].TarifName.Should().Be("Pemeriksaan USG");
        list[2].Harga.Should().Be(250000m);

        _nilaiTarifRepoMock.Verify(x => x.Search(
            It.Is<ILayananKey>(l => l.LayananId == "POLI-01"),
            It.Is<INilaiTarifVariant>(v => v.KelasId == "KLS-01" && v.TipeTarifId == "TT-01"),
            "tindakan"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepoReturnsNull_ReturnsEmptyList()
    {
        // Arrange
        var query = new TrfListTarifRajalQuery("POLI-01", "KLS-01", "TT-01", "keyword");

        _nilaiTarifRepoMock
            .Setup(x => x.Search(It.IsAny<ILayananKey>(), It.IsAny<INilaiTarifVariant>(), It.IsAny<string>()))
            .Returns((IEnumerable<NilaiTarifView>)null!);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
