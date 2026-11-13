using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.UseCases;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AktivasiBerkasFeature.UseCases;

public class AktivasiBerkasQueriesTest
{
    private readonly Mock<IAktivasiBerkasEmrGateway> _gatewayMock;

    public AktivasiBerkasQueriesTest()
    {
        _gatewayMock = new Mock<IAktivasiBerkasEmrGateway>();
    }

    [Theory]
    [InlineData("", "2026-11-09")]
    [InlineData("2026-11-01", "")]
    [InlineData("   ", "2026-11-09")]
    [InlineData("2026-11-01", "   ")]
    public async Task AktivasiBerkasListHandler_ShouldReturnEmpty_WhenDateRangeInvalid(string tglAwal, string tglAkhir)
    {
        // Arrange
        var handler = new AktivasiBerkasListHandler(_gatewayMock.Object);
        var query = new AktivasiBerkasListQuery(tglAwal, tglAkhir);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _gatewayMock.Verify(g => g.ListData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AktivasiBerkasListHandler_ShouldCallGateway_WhenDateRangeValid()
    {
        // Arrange
        var expectedItems = new List<AktivasiBerkasListItemDto>
        {
            new()
            {
                AktifasiBerkasId = "AKT-001",
                TglJam = "2026-11-09 10:00:00",
                ReqUserrId = "USR-01",
                ReqUserrName = "Dr. Test",
                TglAktif = "2026-11-09",
                LayananId = "LAY-01",
                LayananName = "Poli Gigi"
            }
        };

        _gatewayMock
            .Setup(g => g.ListData("2026-11-01", "2026-11-09", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        var handler = new AktivasiBerkasListHandler(_gatewayMock.Object);
        var query = new AktivasiBerkasListQuery("2026-11-01", "2026-11-09");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedItems);
        _gatewayMock.Verify(g => g.ListData("2026-11-01", "2026-11-09", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AktivasiBerkasGetHandler_ShouldReturnNull_WhenIdInvalid(string id)
    {
        // Arrange
        var handler = new AktivasiBerkasGetHandler(_gatewayMock.Object);
        var query = new AktivasiBerkasGetQuery(id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _gatewayMock.Verify(g => g.GetData(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AktivasiBerkasGetHandler_ShouldCallGateway_WhenIdValid()
    {
        // Arrange
        var expectedDetail = new AktivasiBerkasDetailDto
        {
            AktifasiBerkasId = "AKT-100",
            TglJam = "2026-11-09 09:30:00",
            ReqUserrId = "REQ-01",
            ReqUserrName = "Petugas RM",
            AktifasiUserrId = "AKT-USR",
            AktifasiUserrName = "Aktivator",
            TglAktif = "2026-11-09",
            LayananId = "LAY-01",
            LayananName = "Poli Umum",
            ListBerkas =
            [
                new AktivasiBerkasItemDetilDto
                {
                    AktifasiBerkasId = "AKT-100",
                    RegId = "REG-999",
                    PasienId = "PX-01",
                    PasienName = "John Doe"
                }
            ]
        };

        _gatewayMock
            .Setup(g => g.GetData("AKT-100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDetail);

        var handler = new AktivasiBerkasGetHandler(_gatewayMock.Object);
        var query = new AktivasiBerkasGetQuery("AKT-100");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedDetail);
        _gatewayMock.Verify(g => g.GetData("AKT-100", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AktivasiBerkasFindRegisterHandler_ShouldReturnEmpty_WhenKeywordInvalid(string keyword)
    {
        // Arrange
        var handler = new AktivasiBerkasFindRegisterHandler(_gatewayMock.Object);
        var query = new AktivasiBerkasFindRegisterQuery(keyword);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _gatewayMock.Verify(g => g.FindRegister(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AktivasiBerkasFindRegisterHandler_ShouldCallGateway_WhenKeywordValid()
    {
        // Arrange
        var expectedRegisters = new List<AktivasiBerkasRegisterItemDto>
        {
            new()
            {
                RegId = "REG-20261109-001",
                KodeMr = "00-11-22",
                NamaPasien = "Jane Doe",
                TglVoid = "3000-01-01"
            }
        };

        _gatewayMock
            .Setup(g => g.FindRegister("Jane", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRegisters);

        var handler = new AktivasiBerkasFindRegisterHandler(_gatewayMock.Object);
        var query = new AktivasiBerkasFindRegisterQuery("Jane");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedRegisters);
        _gatewayMock.Verify(g => g.FindRegister("Jane", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AktivasiBerkasListReaktifHandler_ShouldCallGateway_WhenLayananIdProvided()
    {
        // Arrange
        var expectedList = new List<AktivasiBerkasListReaktifDto>
        {
            new("LAY01", "Poli Gigi", [
                new AktivasiBerkasItemDetilDto
                {
                    AktifasiBerkasId = "AKT-001",
                    RegId = "REG-001",
                    PasienId = "PX-001",
                    PasienName = "Budi Santoso"
                }
            ])
        };

        _gatewayMock
            .Setup(g => g.ListReaktif("LAY01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        var handler = new AktivasiBerkasListReaktifHandler(_gatewayMock.Object);
        var query = new AktivasiBerkasListReaktifQuery("LAY01");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedList);
        _gatewayMock.Verify(g => g.ListReaktif("LAY01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AktivasiBerkasListReaktifHandler_ShouldCallGateway_WhenLayananIdIsNull()
    {
        // Arrange
        var expectedList = new List<AktivasiBerkasListReaktifDto>();

        _gatewayMock
            .Setup(g => g.ListReaktif(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        var handler = new AktivasiBerkasListReaktifHandler(_gatewayMock.Object);
        var query = new AktivasiBerkasListReaktifQuery(null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _gatewayMock.Verify(g => g.ListReaktif(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AktivasiBerkasListReaktifHandler_ShouldReturnEmpty_WhenRequestIsNull()
    {
        // Arrange
        var handler = new AktivasiBerkasListReaktifHandler(_gatewayMock.Object);

        // Act
        var result = await handler.Handle(null!, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _gatewayMock.Verify(g => g.ListReaktif(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
