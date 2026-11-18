using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class TujuanTransportUseCaseTests
{
    private readonly Mock<ITujuanTransportRepo> _repoMock = new();

    [Fact]
    public async Task TujuanTransportSaveHandler_GivenNew_CreatesAndSaves()
    {
        // Arrange
        _repoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITujuanTransportKey>()))
            .Returns(MayBe<TujuanTransportModel>.None);

        var handler = new TujuanTransportSaveHandler(_repoMock.Object);
        var cmd = new TujuanTransportSaveCommand("TJ-01", "Bandara", 250000m, false, "AMB-01");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.TujuanTransportId.Should().Be("TJ-01");
        _repoMock.Verify(x => x.SaveChanges(It.Is<TujuanTransportModel>(t =>
            t.TujuanTransportId == "TJ-01" &&
            t.TujuanTransportName == "Bandara" &&
            t.Konstanta == 250000m &&
            !t.IsPerkiraan &&
            t.DefaultAmbulanceId == "AMB-01")), Times.Once);
    }

    [Fact]
    public async Task TujuanTransportSaveHandler_GivenExisting_UpdatesAndSaves()
    {
        // Arrange
        var existing = new TujuanTransportModel("TJ-01", "Old Name", 100000m, false);
        _repoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITujuanTransportKey>()))
            .Returns(MayBe.From(existing));

        var handler = new TujuanTransportSaveHandler(_repoMock.Object);
        var cmd = new TujuanTransportSaveCommand("TJ-01", "New Name", 200000m, true, "AMB-02");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.TujuanTransportId.Should().Be("TJ-01");
        existing.TujuanTransportName.Should().Be("New Name");
        existing.Konstanta.Should().Be(200000m);
        existing.IsPerkiraan.Should().BeTrue();
        existing.DefaultAmbulanceId.Should().Be("AMB-02");
        _repoMock.Verify(x => x.SaveChanges(existing), Times.Once);
    }

    [Fact]
    public async Task TujuanTransportDeleteHandler_DeletesFromRepo()
    {
        // Arrange
        var handler = new TujuanTransportDeleteHandler(_repoMock.Object);

        // Act
        await handler.Handle(new TujuanTransportDeleteCommand("TJ-01"), CancellationToken.None);

        // Assert
        _repoMock.Verify(x => x.Delete(It.Is<ITujuanTransportKey>(k => k.TujuanTransportId == "TJ-01")), Times.Once);
    }

    [Fact]
    public async Task DefaultAmbulance_Set_And_Unset_Success()
    {
        // Arrange
        var existing = new TujuanTransportModel("TJ-01", "RS Rujukan", 150000m, false);
        _repoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITujuanTransportKey>()))
            .Returns(MayBe.From(existing));

        var setHandler = new TujuanTransportSetDefaultAmbulanceHandler(_repoMock.Object);
        var unsetHandler = new TujuanTransportUnSetDefaultAmbulanceHandler(_repoMock.Object);

        // Act & Assert Set
        await setHandler.Handle(new TujuanTransportSetDefaultAmbulanceCommand("TJ-01", "AMB-01"), CancellationToken.None);
        existing.DefaultAmbulanceId.Should().Be("AMB-01");

        // Act & Assert Unset
        await unsetHandler.Handle(new TujuanTransportUnSetDefaultAmbulanceCommand("TJ-01"), CancellationToken.None);
        existing.DefaultAmbulanceId.Should().BeEmpty();
    }

    [Fact]
    public async Task TujuanTransportGetHandler_ReturnsMappedResponse()
    {
        // Arrange
        var existing = new TujuanTransportModel("TJ-01", "Stasiun", 75000m, true);
        existing.SetDefaultAmbulance("AMB-03");

        _repoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITujuanTransportKey>()))
            .Returns(MayBe.From(existing));

        var handler = new TujuanTransportGetHandler(_repoMock.Object);

        // Act
        var result = await handler.Handle(new TujuanTransportGetQuery("TJ-01"), CancellationToken.None);

        // Assert
        result.TujuanTransportId.Should().Be("TJ-01");
        result.TujuanTransportName.Should().Be("Stasiun");
        result.Konstanta.Should().Be(75000m);
        result.IsPerkiraan.Should().BeTrue();
        result.DefaultAmbulanceId.Should().Be("AMB-03");
    }

    [Fact]
    public async Task TujuanTransportListHandler_ReturnsAll()
    {
        // Arrange
        var list = new List<TujuanTransportModel>
        {
            new("TJ-01", "Tujuan 1", 1000m, false),
            new("TJ-02", "Tujuan 2", 2000m, true)
        };
        _repoMock.Setup(x => x.ListData()).Returns(list);

        var handler = new TujuanTransportListHandler(_repoMock.Object);

        // Act
        var result = await handler.Handle(new ListTujuanTransportQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }
}
