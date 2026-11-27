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

public class PreviewTransportTariffQueryTest
{
    private readonly Mock<IAmbulanceRepo> _ambulanceRepoMock = new();
    private readonly Mock<ITujuanTransportRepo> _tujuanRepoMock = new();
    private readonly Mock<IKomponenRepo> _komponenRepoMock = new();

    [Fact]
    public async Task Handle_WhenValid_ReturnsCalculatedTariffWithKomponenNames()
    {
        // Arrange
        var ambulance = new AmbulanceModel("AMB-01", "Ambulans 01");
        ambulance.SetAbonement(20000m);
        ambulance.Attach([
            new AmbulanceKomponenModel("AMB-01", "K-TETAP", 50000m, true),
            new AmbulanceKomponenModel("AMB-01", "K-VAR", 100000m, false)
        ]);
        ambulance.AddTujuan(new TransportTujuanModel("AMB-01", "TJ-01", 3m));

        var tujuan = new TujuanTransportModel("TJ-01", "Semarang", 2m, false);

        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.Is<IAmbulanceKey>(k => k.AmbulanceId == "AMB-01")))
            .Returns(MayBe.From(ambulance));
        _tujuanRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITujuanTransportKey>(k => k.TujuanTransportId == "TJ-01")))
            .Returns(MayBe.From(tujuan));
        _komponenRepoMock
            .Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([
                KomponenType.Default with { KomponenId = "K-TETAP", KomponenName = "Administrasi" },
                KomponenType.Default with { KomponenId = "K-VAR", KomponenName = "BBM" }
            ]);

        var handler = new PreviewTransportTariffHandler(
            _ambulanceRepoMock.Object,
            _tujuanRepoMock.Object,
            _komponenRepoMock.Object);

        // Act
        var result = await handler.Handle(new PreviewTransportTariffQuery("AMB-01", "TJ-01"), CancellationToken.None);

        // Assert
        result.AmbulanceId.Should().Be("AMB-01");
        result.TujuanTransportId.Should().Be("TJ-01");
        result.Konstanta.Should().Be(3m);
        result.Abonement.Should().Be(20000m);
        result.Total.Should().Be(370000m);
        result.ListKomponen.Should().HaveCount(2);
        result.ListKomponen.First(x => x.DetilTarifId == "K-TETAP").DetilTarifName.Should().Be("Administrasi");
        result.ListKomponen.First(x => x.DetilTarifId == "K-TETAP").Tarif.Should().Be(50000m);
        result.ListKomponen.First(x => x.DetilTarifId == "K-VAR").DetilTarifName.Should().Be("BBM");
        result.ListKomponen.First(x => x.DetilTarifId == "K-VAR").Tarif.Should().Be(320000m);
    }

    [Fact]
    public async Task Handle_WhenAmbulanceNotFound_ThrowsKeyNotFoundException()
    {
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IAmbulanceKey>()))
            .Returns(MayBe<AmbulanceModel>.None);

        var handler = new PreviewTransportTariffHandler(
            _ambulanceRepoMock.Object,
            _tujuanRepoMock.Object,
            _komponenRepoMock.Object);

        var act = () => handler.Handle(new PreviewTransportTariffQuery("AMB-MISSING", "TJ-01"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTujuanNotFound_ThrowsKeyNotFoundException()
    {
        var ambulance = new AmbulanceModel("AMB-01", "Ambulans 01");
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IAmbulanceKey>()))
            .Returns(MayBe.From(ambulance));
        _tujuanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITujuanTransportKey>()))
            .Returns(MayBe<TujuanTransportModel>.None);

        var handler = new PreviewTransportTariffHandler(
            _ambulanceRepoMock.Object,
            _tujuanRepoMock.Object,
            _komponenRepoMock.Object);

        var act = () => handler.Handle(new PreviewTransportTariffQuery("AMB-01", "TJ-MISSING"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
