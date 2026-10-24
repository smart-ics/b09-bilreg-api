using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WardCockpitFeature;

public class WardCockpitBedDetailQueryHandlerTest
{
    private readonly Mock<IWardCockpitDal> _dalMock;
    private readonly WardCockpitBedDetailQueryHandler _sut;

    public WardCockpitBedDetailQueryHandlerTest()
    {
        _dalMock = new Mock<IWardCockpitDal>();
        _sut = new WardCockpitBedDetailQueryHandler(_dalMock.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_GivenNullOrWhiteSpaceBedId_ThrowsArgumentException(string? invalidBedId)
    {
        // Arrange
        var query = new WardCockpitBedDetailQuery(invalidBedId!);

        // Act
        var act = () => _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _dalMock.Verify(x => x.GetBedDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenValidBedId_CallsDalAndReturnsBedDetailDto()
    {
        // Arrange
        var bedId = "BED-101-A";
        var expectedDto = new WardCockpitBedDetailDto(
            BedInfo: new WardCockpitBedDetailBedInfoDto("BED-101-A", "101-A", "Kamar 101", "Kelas III"),
            Occupancy: new WardCockpitBedDetailOccupancyDto(
                PakaiBedId: "PKB-20261024-001",
                RegId: "REG-20261024-005",
                Patient: new WardCockpitBedDetailPatientDto("MR-001234", "Ahmad Wijaya", "Laki-laki", "1981-05-12", "45 Tahun"),
                Penjamin: new WardCockpitBedDetailPenjaminDto("BPJS", "BPJS Kesehatan", "00012345678"),
                Dpjp: new WardCockpitBedDetailDpjpDto("DOK-01", "dr. Sutomo, Sp.PD"),
                CheckInTime: new DateTime(2026, 10, 21, 8, 30, 0),
                LosDays: 3,
                BillingKelas: "Kelas III"
            ),
            TabTindakan:
            [
                new WardCockpitBedDetailTindakanDto(
                    new DateTime(2026, 10, 23, 10, 0, 0),
                    "Pemasangan Infus & Cairan RL",
                    "Ns. Ratna",
                    125000m
                )
            ],
            TabRoomCharge: new WardCockpitBedDetailRoomChargeDto(
                TarifPerHari: 250000m,
                AkumulasiHari: 3,
                EstimasiTotalSewa: 750000m,
                CutOffTime: "12:00"
            ),
            TabRiwayatMutasi:
            [
                new WardCockpitBedDetailRiwayatMutasiDto(
                    new DateTime(2026, 10, 21, 8, 30, 0),
                    "Check-in Kamar 101 (101-A)",
                    "Ns. Ratna"
                )
            ]
        );

        _dalMock
            .Setup(x => x.GetBedDetailAsync(bedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var query = new WardCockpitBedDetailQuery("  BED-101-A  ");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        result!.BedInfo.BedId.Should().Be("BED-101-A");
        result.Occupancy.Should().NotBeNull();
        result.Occupancy!.Patient.NamaPasien.Should().Be("Ahmad Wijaya");
        result.Occupancy.Penjamin.NoKartu.Should().Be("00012345678");
        result.TabTindakan.Should().HaveCount(1);
        result.TabRoomCharge.Should().NotBeNull();
        result.TabRoomCharge!.EstimasiTotalSewa.Should().Be(750000m);
        result.TabRiwayatMutasi.Should().HaveCount(1);

        _dalMock.Verify(x => x.GetBedDetailAsync("BED-101-A", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenUnoccupiedBed_ReturnsDtoWithNullOccupancyAndEmptyTabs()
    {
        // Arrange
        var bedId = "BED-101-B";
        var expectedDto = new WardCockpitBedDetailDto(
            BedInfo: new WardCockpitBedDetailBedInfoDto("BED-101-B", "101-B", "Kamar 101", "Kelas III"),
            Occupancy: null,
            TabTindakan: [],
            TabRoomCharge: null,
            TabRiwayatMutasi: []
        );

        _dalMock
            .Setup(x => x.GetBedDetailAsync(bedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var query = new WardCockpitBedDetailQuery(bedId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.BedInfo.BedId.Should().Be("BED-101-B");
        result.Occupancy.Should().BeNull();
        result.TabTindakan.Should().BeEmpty();
        result.TabRoomCharge.Should().BeNull();
        result.TabRiwayatMutasi.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GivenNonExistentBedId_ReturnsNull()
    {
        // Arrange
        _dalMock
            .Setup(x => x.GetBedDetailAsync("BED-NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardCockpitBedDetailDto?)null);

        var query = new WardCockpitBedDetailQuery("BED-NONEXISTENT");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
