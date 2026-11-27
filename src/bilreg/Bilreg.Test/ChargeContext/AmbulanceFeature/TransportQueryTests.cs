using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class TransportQueryTests
{
    private readonly Mock<ITrsTransportRepo> _trsTransportRepoMock = new();
    private readonly Mock<IKomponenRepo> _komponenRepoMock = new();

    private static TrsTransportModel CreatePopulatedTransport(string id = "TP030")
    {
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            id,
            new DateTime(2026, 11, 16, 9, 0, 0),
            regId: "REG-01",
            tujuanTransportId: "TJ-01",
            konstanta: 150000m,
            lokasiAsal: "RS IGD",
            lokasiTujuan: "RS Kariadi",
            jaminanId: "BPJS",
            keterangan: "Rujukan");

        transport.Dispatch(
            "AMB-01",
            "DRV-01",
            "B 1234 CD",
            1000m,
            perawatId: "PRW-01",
            dokterId: "DR-01",
            dispatchTime: new DateTime(2026, 11, 16, 9, 15, 0));

        transport.RecordArrival(new DateTime(2026, 11, 16, 10, 0, 0));

        transport.AddItem(new TrsTransportItemModel(id, "KOMP-01", 150000m, 1m, "PRW-01", 0m, "Tarif Dasar"));
        transport.AddItem(new TrsTransportItemModel(id, "KOMP-02", 50000m, 1m, "DR-01", 5000m, "Jasa Medis"));

        transport.Complete(1050m, new DateTime(2026, 11, 16, 11, 0, 0));

        return transport;
    }

    [Fact]
    public async Task GetTransportQuery_WhenFound_ReturnsFullDetails()
    {
        // Arrange
        var transport = CreatePopulatedTransport("TP030");
        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP030")))
            .Returns(MayBe.From(transport));

        _komponenRepoMock
            .Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([
                KomponenType.Default with { KomponenId = "KOMP-01", KomponenName = "Jasa Sarana" },
                KomponenType.Default with { KomponenId = "KOMP-02", KomponenName = "Jasa Pelayanan" }
            ]);

        var handler = new GetTransportHandler(_trsTransportRepoMock.Object, _komponenRepoMock.Object);

        // Act
        var result = await handler.Handle(new GetTransportQuery("TP030"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TrsTransportId.Should().Be("TP030");
        result.RegId.Should().Be("REG-01");
        result.LayananId.Should().Be("IGD");
        result.Status.Should().Be("Completed");
        result.TipePerjalanan.Should().Be("OutboundReferral");
        result.MetodeTarif.Should().Be("Wilayah");
        result.AmbulanceId.Should().Be("AMB-01");
        result.DriverId.Should().Be("DRV-01");
        result.NoPolisi.Should().Be("B 1234 CD");
        result.PerawatId.Should().Be("PRW-01");
        result.DokterId.Should().Be("DR-01");
        result.LokasiAsal.Should().Be("RS IGD");
        result.LokasiTujuan.Should().Be("RS Kariadi");
        result.StartOdometer.Should().Be(1000m);
        result.EndOdometer.Should().Be(1050m);
        result.TotalJarakKm.Should().Be(50m);
        result.SubTotal.Should().Be(200000m);
        result.Diskon.Should().Be(5000m);
        result.Total.Should().Be(195000m);
        result.ListItems.Should().HaveCount(2);
        result.ListItems.First(i => i.DetilTarifId == "KOMP-01").DetilTarifName.Should().Be("Jasa Sarana");
        result.ListItems.First(i => i.DetilTarifId == "KOMP-02").DetilTarifName.Should().Be("Jasa Pelayanan");
    }

    [Fact]
    public async Task GetTransportQuery_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITrsTransportKey>()))
            .Returns(MayBe<TrsTransportModel>.None);

        _komponenRepoMock
            .Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([
                KomponenType.Default with { KomponenId = "KOMP-01", KomponenName = "Jasa Sarana" },
                KomponenType.Default with { KomponenId = "KOMP-02", KomponenName = "Jasa Pelayanan" }
            ]);

        var handler = new GetTransportHandler(_trsTransportRepoMock.Object, _komponenRepoMock.Object);

        // Act
        var act = () => handler.Handle(new GetTransportQuery("TP-NONEXIST"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ListTransportByRegQuery_ReturnsTransportsForPatient()
    {
        // Arrange
        var transport = CreatePopulatedTransport("TP031");
        _trsTransportRepoMock
            .Setup(x => x.ListDataByReg("REG-01"))
            .Returns([transport]);

        _komponenRepoMock
            .Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([
                KomponenType.Default with { KomponenId = "KOMP-01", KomponenName = "Jasa Sarana" },
                KomponenType.Default with { KomponenId = "KOMP-02", KomponenName = "Jasa Pelayanan" }
            ]);

        var handler = new ListTransportByRegHandler(_trsTransportRepoMock.Object, _komponenRepoMock.Object);

        // Act
        var result = await handler.Handle(new ListTransportByRegQuery("REG-01"), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().TrsTransportId.Should().Be("TP031");
        result.First().RegId.Should().Be("REG-01");
    }

    [Fact]
    public async Task ListActiveDispatchQuery_ReturnsCurrentlyDispatchedTransports()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.InboundRetrieval,
            ChargingMethodEnum.Jarak,
            "IGD",
            "USER1",
            "TP032",
            DateTime.Now);

        transport.Dispatch("AMB-02", "DRV-02", "B 5678 EF", 200m);

        _trsTransportRepoMock
            .Setup(x => x.ListActiveDispatched())
            .Returns([transport]);

        var handler = new ListActiveDispatchHandler(_trsTransportRepoMock.Object);

        // Act
        var result = await handler.Handle(new ListActiveDispatchQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().TrsTransportId.Should().Be("TP032");
        result.First().Status.Should().Be("Dispatched");
    }

    [Fact]
    public void TransportResponseMapper_WhenVoided_MapsIsVoidTrue()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP033",
            new DateTime(2026, 11, 16, 9, 0, 0),
            tujuanTransportId: "TJ-01",
            konstanta: 150000m);
        transport.Cancel("Salah input", 0m, "USER1", new DateTime(2026, 11, 16, 12, 0, 0));

        // Act
        var response = TransportResponseMapper.ToResponse(transport);

        // Assert
        response.IsVoid.Should().BeTrue();
        response.Status.Should().Be("Cancelled");
    }
}
