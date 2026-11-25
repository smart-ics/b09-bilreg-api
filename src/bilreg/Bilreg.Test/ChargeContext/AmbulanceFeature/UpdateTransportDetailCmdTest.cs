using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class UpdateTransportDetailCmdTest
{
    private readonly Mock<ITrsTransportRepo> _trsTransportRepoMock = new();
    private readonly UpdateTransportDetailHandler _sut;

    public UpdateTransportDetailCmdTest()
    {
        _sut = new UpdateTransportDetailHandler(_trsTransportRepoMock.Object);
    }

    private static TrsTransportModel CreateTransport(string id = "TP-001")
    {
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            trsTransportId: id,
            tglTrs: new DateTime(2026, 11, 19, 9, 0, 0),
            regId: "REG-001",
            tujuanTransportId: "TUJ01",
            konstanta: 3m,
            ambulanceId: "AMB01",
            driverId: "DRV-OLD",
            perawatId: "PRW-OLD",
            dokterId: "DOK-OLD");

        transport.AddItem(new TrsTransportItemModel(id, "K-TETAP", 50000m, 1m));
        transport.AddItem(new TrsTransportItemModel(id, "K-VAR", 60000m, 1m));
        return transport;
    }

    [Fact]
    public async Task Handle_ValidCrewAndItemAdjustments_UpdatesAndSavesTransport()
    {
        // Arrange
        var transport = CreateTransport("TP-001");
        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP-001")))
            .Returns(MayBe.From(transport));

        var cmd = new UpdateTransportDetailCmd(
            "TP-001",
            DriverId: "DRV-NEW",
            PerawatId: "",
            DokterId: null,
            Items: [new TransportItemAdjustmentDto("K-VAR", 75000m, 5000m)]);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TrsTransportId.Should().Be("TP-001");
        result.DriverId.Should().Be("DRV-NEW");
        result.PerawatId.Should().Be(string.Empty);
        result.DokterId.Should().Be("DOK-OLD");
        result.SubTotal.Should().Be(125000m);
        result.Diskon.Should().Be(5000m);
        result.Total.Should().Be(120000m);
        _trsTransportRepoMock.Verify(x => x.SaveChanges(transport), Times.Once);
    }

    [Fact]
    public async Task Handle_TransportNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITrsTransportKey>()))
            .Returns(MayBe<TrsTransportModel>.None);

        var cmd = new UpdateTransportDetailCmd("TP-404", DriverId: "DRV-1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyPostedBilling_ThrowsInvalidOperationException()
    {
        // Arrange
        var transport = CreateTransport("TP-002");
        transport.SetPostedBilling();

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP-002")))
            .Returns(MayBe.From(transport));

        var cmd = new UpdateTransportDetailCmd("TP-002", DriverId: "DRV-NEW");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_DefaultEmptyTrsTransportId_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new UpdateTransportDetailCmd(DriverId: "DRV-NEW");
        cmd.TrsTransportId.Should().BeEmpty();

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
