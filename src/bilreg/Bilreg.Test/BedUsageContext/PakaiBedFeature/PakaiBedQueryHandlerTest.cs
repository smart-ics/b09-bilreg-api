using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedQueryHandlerTest
{
    private static readonly DateTime MasukTime1 = new(2026, 10, 16, 8, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime MasukTime2 = new(2026, 10, 16, 9, 15, 0, DateTimeKind.Utc);
    private readonly Mock<IPakaiBedActiveOccupancyDal> _dal = new();

    #region PakaiBedGetActiveByBedQuery Tests

    [Fact]
    public async Task GetActiveByBed_WhenBedOccupied_ReturnsActiveOccupancyDetails()
    {
        // Arrange
        var expected = new PakaiBedActiveOccupancyResponse(
            PakaiBedId: "PKB000000001",
            RegId: "REG00001",
            PasienId: "PAS001",
            PasienName: "PASIEN MAWAR",
            BangsalId: "BG01",
            BangsalName: "BANGSAL MAWAR",
            KamarId: "KMR01",
            KamarName: "KAMAR 101",
            BedId: "BED-01",
            BedName: "BED-01A",
            TglJamMasuk: MasukTime1,
            BillingKelasId: "KL1",
            BillingKelasName: "KELAS 1",
            IsRoomingIn: false,
            MotherPakaiBedId: string.Empty);

        _dal.Setup(x => x.GetActiveByBed("BED-01"))
            .Returns(expected);

        var handler = new PakaiBedGetActiveByBedQueryHandler(_dal.Object);

        // Act
        var result = await handler.Handle(new PakaiBedGetActiveByBedQuery("BED-01"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PakaiBedId.Should().Be("PKB000000001");
        result.BedId.Should().Be("BED-01");
        result.BedName.Should().Be("BED-01A");
        result.NomorBed.Should().Be("BED-01A");
        result.PasienName.Should().Be("PASIEN MAWAR");
        result.NamaPasien.Should().Be("PASIEN MAWAR");
        result.TglJamMasuk.Should().Be(MasukTime1);
        result.BillingKelasId.Should().Be("KL1");
        result.BillingKelasName.Should().Be("KELAS 1");
        result.BillingClass.Should().Be("KL1");
        result.IsRoomingIn.Should().BeFalse();
        result.StatusRawatGabung.Should().BeFalse();
        result.MotherPakaiBedId.Should().BeEmpty();
        _dal.Verify(x => x.GetActiveByBed("BED-01"), Times.Once);
    }

    [Fact]
    public async Task GetActiveByBed_WhenRoomingIn_ReturnsStatusRawatGabungTrue()
    {
        // Arrange
        var expected = new PakaiBedActiveOccupancyResponse(
            PakaiBedId: "PKB000000002",
            RegId: "REG00002",
            PasienId: "PAS002",
            PasienName: "BAYI NY MAWAR",
            BangsalId: "BG01",
            BangsalName: "BANGSAL MAWAR",
            KamarId: "KMR01",
            KamarName: "KAMAR 101",
            BedId: "BED-01",
            BedName: "BED-01A",
            TglJamMasuk: MasukTime2,
            BillingKelasId: "KL1",
            BillingKelasName: "KELAS 1",
            IsRoomingIn: true,
            MotherPakaiBedId: "PKB000000001");

        _dal.Setup(x => x.GetActiveByBed("BED-01"))
            .Returns(expected);

        var handler = new PakaiBedGetActiveByBedQueryHandler(_dal.Object);

        // Act
        var result = await handler.Handle(new PakaiBedGetActiveByBedQuery("BED-01"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IsRoomingIn.Should().BeTrue();
        result.StatusRawatGabung.Should().BeTrue();
        result.MotherPakaiBedId.Should().Be("PKB000000001");
    }

    [Fact]
    public async Task GetActiveByBed_WhenBedEmpty_ReturnsNull()
    {
        // Arrange
        _dal.Setup(x => x.GetActiveByBed("BED-99"))
            .Returns((PakaiBedActiveOccupancyResponse?)null);

        var handler = new PakaiBedGetActiveByBedQueryHandler(_dal.Object);

        // Act
        var result = await handler.Handle(new PakaiBedGetActiveByBedQuery("BED-99"), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _dal.Verify(x => x.GetActiveByBed("BED-99"), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetActiveByBed_WhenBedIdEmptyOrWhitespace_ReturnsNullWithoutCallingDal(string? invalidBedId)
    {
        // Arrange
        var handler = new PakaiBedGetActiveByBedQueryHandler(_dal.Object);

        // Act
        var result = await handler.Handle(new PakaiBedGetActiveByBedQuery(invalidBedId!), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _dal.Verify(x => x.GetActiveByBed(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region PakaiBedListActiveByWardQuery Tests

    [Fact]
    public async Task ListActiveByWard_WhenWardHasOccupants_ReturnsAllOccupiedBeds()
    {
        // Arrange
        var item1 = new PakaiBedActiveOccupancyResponse(
            PakaiBedId: "PKB000000001",
            RegId: "REG00001",
            PasienId: "PAS001",
            PasienName: "PASIEN MAWAR",
            BangsalId: "BG01",
            BangsalName: "BANGSAL MAWAR",
            KamarId: "KMR01",
            KamarName: "KAMAR 101",
            BedId: "BED-01",
            BedName: "BED-01A",
            TglJamMasuk: MasukTime1,
            BillingKelasId: "KL1",
            BillingKelasName: "KELAS 1",
            IsRoomingIn: false,
            MotherPakaiBedId: string.Empty);

        var item2 = new PakaiBedActiveOccupancyResponse(
            PakaiBedId: "PKB000000002",
            RegId: "REG00002",
            PasienId: "PAS002",
            PasienName: "BAYI NY MAWAR",
            BangsalId: "BG01",
            BangsalName: "BANGSAL MAWAR",
            KamarId: "KMR01",
            KamarName: "KAMAR 101",
            BedId: "BED-01",
            BedName: "BED-01A",
            TglJamMasuk: MasukTime2,
            BillingKelasId: "KL1",
            BillingKelasName: "KELAS 1",
            IsRoomingIn: true,
            MotherPakaiBedId: "PKB000000001");

        var item3 = new PakaiBedActiveOccupancyResponse(
            PakaiBedId: "PKB000000003",
            RegId: "REG00003",
            PasienId: "PAS003",
            PasienName: "PASIEN MELATI",
            BangsalId: "BG01",
            BangsalName: "BANGSAL MAWAR",
            KamarId: "KMR02",
            KamarName: "KAMAR 102",
            BedId: "BED-02",
            BedName: "BED-02B",
            TglJamMasuk: MasukTime1,
            BillingKelasId: "KL2",
            BillingKelasName: "KELAS 2",
            IsRoomingIn: false,
            MotherPakaiBedId: string.Empty);

        var expectedList = new List<PakaiBedActiveOccupancyResponse> { item1, item2, item3 };

        _dal.Setup(x => x.ListActiveByWard("BG01"))
            .Returns(expectedList);

        var handler = new PakaiBedListActiveByWardQueryHandler(_dal.Object);

        // Act
        var result = (await handler.Handle(new PakaiBedListActiveByWardQuery("BG01"), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedList);

        // Verify Criterion 3: informasi nomor bed, nama pasien, tanggal jam masuk, billing class, dan status rawat gabung
        result[0].BedName.Should().Be("BED-01A");
        result[0].PasienName.Should().Be("PASIEN MAWAR");
        result[0].TglJamMasuk.Should().Be(MasukTime1);
        result[0].BillingKelasId.Should().Be("KL1");
        result[0].IsRoomingIn.Should().BeFalse();

        result[1].BedName.Should().Be("BED-01A");
        result[1].PasienName.Should().Be("BAYI NY MAWAR");
        result[1].TglJamMasuk.Should().Be(MasukTime2);
        result[1].BillingKelasId.Should().Be("KL1");
        result[1].IsRoomingIn.Should().BeTrue();

        result[2].BedName.Should().Be("BED-02B");
        result[2].PasienName.Should().Be("PASIEN MELATI");
        result[2].TglJamMasuk.Should().Be(MasukTime1);
        result[2].BillingKelasId.Should().Be("KL2");
        result[2].IsRoomingIn.Should().BeFalse();

        _dal.Verify(x => x.ListActiveByWard("BG01"), Times.Once);
    }

    [Fact]
    public async Task ListActiveByWard_WhenWardEmpty_ReturnsEmptyCollection()
    {
        // Arrange
        _dal.Setup(x => x.ListActiveByWard("BG99"))
            .Returns([]);

        var handler = new PakaiBedListActiveByWardQueryHandler(_dal.Object);

        // Act
        var result = await handler.Handle(new PakaiBedListActiveByWardQuery("BG99"), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _dal.Verify(x => x.ListActiveByWard("BG99"), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ListActiveByWard_WhenBangsalIdEmptyOrWhitespace_ReturnsEmptyCollectionWithoutCallingDal(string? invalidBangsalId)
    {
        // Arrange
        var handler = new PakaiBedListActiveByWardQueryHandler(_dal.Object);

        // Act
        var result = await handler.Handle(new PakaiBedListActiveByWardQuery(invalidBangsalId!), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _dal.Verify(x => x.ListActiveByWard(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Response Mapping & Properties Tests

    [Fact]
    public void Response_PropertiesAndAliases_MatchExpectedValues()
    {
        var response = new PakaiBedActiveOccupancyResponse(
            PakaiBedId: "PKB001",
            RegId: "REG001",
            PasienId: "PAS001",
            PasienName: "PASIEN MAWAR",
            BangsalId: "BG01",
            BangsalName: "MAWAR",
            KamarId: "KMR01",
            KamarName: "101",
            BedId: "BED01",
            BedName: "01A",
            TglJamMasuk: MasukTime1,
            BillingKelasId: "KL1",
            BillingKelasName: "KELAS 1",
            IsRoomingIn: true,
            MotherPakaiBedId: "PKB-MOM");

        response.NomorBed.Should().Be("01A");
        response.NamaPasien.Should().Be("PASIEN MAWAR");
        response.BillingClass.Should().Be("KL1");
        response.StatusRawatGabung.Should().BeTrue();
    }

    [Fact]
    public void Dto_ToResponse_MapsAllFieldsCorrectly()
    {
        var dto = new Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature.PakaiBedActiveOccupancyDto
        {
            PakaiBedId = "PKB001",
            RegId = "REG001",
            PasienId = "PAS001",
            PasienName = "PASIEN MAWAR",
            BangsalId = "BG01",
            BangsalName = "MAWAR",
            KamarId = "KMR01",
            KamarName = "101",
            BedId = "BED01",
            BedName = "01A",
            TglJamMasuk = MasukTime1,
            BillingKelasId = "KL1",
            BillingKelasName = "KELAS 1",
            IsRoomingIn = false,
            MotherPakaiBedId = string.Empty
        };

        var response = dto.ToResponse();

        response.PakaiBedId.Should().Be(dto.PakaiBedId);
        response.RegId.Should().Be(dto.RegId);
        response.PasienId.Should().Be(dto.PasienId);
        response.PasienName.Should().Be(dto.PasienName);
        response.BangsalId.Should().Be(dto.BangsalId);
        response.BangsalName.Should().Be(dto.BangsalName);
        response.KamarId.Should().Be(dto.KamarId);
        response.KamarName.Should().Be(dto.KamarName);
        response.BedId.Should().Be(dto.BedId);
        response.BedName.Should().Be(dto.BedName);
        response.TglJamMasuk.Should().Be(dto.TglJamMasuk);
        response.BillingKelasId.Should().Be(dto.BillingKelasId);
        response.BillingKelasName.Should().Be(dto.BillingKelasName);
        response.IsRoomingIn.Should().BeFalse();
        response.MotherPakaiBedId.Should().BeEmpty();
    }

    #endregion
}

