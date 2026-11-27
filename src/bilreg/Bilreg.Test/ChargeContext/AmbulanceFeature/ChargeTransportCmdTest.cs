using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class ChargeTransportCmdTest
{
    private readonly Mock<ITrsTransportRepo> _trsTransportRepoMock = new();
    private readonly Mock<IAmbulanceRepo> _ambulanceRepoMock = new();
    private readonly Mock<ITujuanTransportRepo> _tujuanTransportRepoMock = new();
    private readonly Mock<IAddBillAppService> _addBillAppServiceMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IJaminanRepo> _jaminanRepoMock = new();
    private readonly Mock<IKomponenRepo> _komponenRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly ChargeTransportHandler _sut;

    public ChargeTransportCmdTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 11, 19, 10, 0, 0));
        _sut = new ChargeTransportHandler(
            _trsTransportRepoMock.Object,
            _ambulanceRepoMock.Object,
            _tujuanTransportRepoMock.Object,
            _addBillAppServiceMock.Object,
            _regRepoMock.Object,
            _jaminanRepoMock.Object,
            _komponenRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    private static RegModel CreateReg(string regId)
    {
        var masukAudit = new AuditInfoType("USER1", new DateTime(2026, 11, 19, 8, 0, 0));
        var tipeJaminan = new TipeJaminanReff("BPJS", "BPJS Kesehatan");

        return new RegModel(
            regId,
            new DateOnly(2026, 11, 19),
            masukAudit,
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            tipeJaminan,
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    [Fact]
    public async Task GivenValidRequestedTransport_WhenHandled_ThenPostsBillingAndSavesChanges()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP010",
            DateTime.Now,
            regId: "REG-01",
            tujuanTransportId: "TJ-01",
            ambulanceId: "AMB-01");

        transport.AddItem(new TrsTransportItemModel("TP010", "KOMP-01", 150000m, 1m));

        var reg = CreateReg("REG-01");
        var ambulance = new AmbulanceModel("AMB-01", "Ambulance Alfa");

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP010")))
            .Returns(MayBe.From(transport));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));

        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.Is<IAmbulanceKey>(k => k.AmbulanceId == "AMB-01")))
            .Returns(MayBe.From(ambulance));

        _jaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(JaminanType.Default));

        var cmd = new ChargeTransportCmd("TP010", "USER_KASIR");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TrsTransportId.Should().Be("TP010");
        result.Status.Should().Be("Requested");
        result.Total.Should().Be(150000m);
        result.IsPostedBilling.Should().BeTrue();

        transport.IsPostedBilling.Should().BeTrue();

        _addBillAppServiceMock.Verify(x => x.FromAmbulanceUsage(
            transport,
            reg,
            It.IsAny<JaminanType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            null,
            It.IsAny<DateTime>()), Times.Once);

        _trsTransportRepoMock.Verify(x => x.SaveChanges(transport), Times.Once);
    }

    [Fact]
    public async Task GivenTransportWithoutItems_WhenHandled_ThenPopulatesComponentsFromAmbulanceMaster()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP011",
            DateTime.Now,
            regId: "REG-01",
            tujuanTransportId: "TJ-01",
            ambulanceId: "AMB-02");

        var reg = CreateReg("REG-01");
        var ambulance = new AmbulanceModel("AMB-02", "Ambulance Beta");
        ambulance.Add(new AmbulanceKomponenModel("AMB-02", "KOMP-SEWA", 200000m, true));
        ambulance.Add(new AmbulanceKomponenModel("AMB-02", "KOMP-MEDIS", 50000m, false));

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP011")))
            .Returns(MayBe.From(transport));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));

        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.Is<IAmbulanceKey>(k => k.AmbulanceId == "AMB-02")))
            .Returns(MayBe.From(ambulance));

        _tujuanTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITujuanTransportKey>(k => k.TujuanTransportId == "TJ-01")))
            .Returns(MayBe.From(new TujuanTransportModel("TJ-01", "RS Tujuan", 1m, false)));

        _jaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(JaminanType.Default));

        var cmd = new ChargeTransportCmd("TP011");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Total.Should().Be(250000m);
        result.IsPostedBilling.Should().BeTrue();
        transport.ListItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task GivenWilayahTransportWithZeroSubTotal_WhenHandled_ThenCalculatesSubtotalFromAbonementAndKonstanta()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP012",
            DateTime.Now,
            regId: "REG-01",
            tujuanTransportId: "TJ-01",
            konstanta: 1.5m,
            ambulanceId: "AMB-03");

        var reg = CreateReg("REG-01");
        var ambulance = new AmbulanceModel("AMB-03", "Ambulance Gamma");
        ambulance.SetAbonement(100000m); // no components — TransportTariffCalculator produces 0 items without components

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP012")))
            .Returns(MayBe.From(transport));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));

        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.Is<IAmbulanceKey>(k => k.AmbulanceId == "AMB-03")))
            .Returns(MayBe.From(ambulance));

        _tujuanTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITujuanTransportKey>(k => k.TujuanTransportId == "TJ-01")))
            .Returns(MayBe.From(new TujuanTransportModel("TJ-01", "RS Tujuan", 1.5m, false)));

        _jaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(JaminanType.Default));

        var cmd = new ChargeTransportCmd("TP012");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Total.Should().Be(0m);
        result.IsPostedBilling.Should().BeTrue();
    }

    [Fact]
    public async Task GivenMissingTransport_WhenHandled_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITrsTransportKey>()))
            .Returns(MayBe<TrsTransportModel>.None);

        var cmd = new ChargeTransportCmd("NONEXISTENT");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task GivenNonRegTransport_WhenHandled_ThenThrowsArgumentException()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP013",
            DateTime.Now,
            regId: "",
            tujuanTransportId: "TJ-01");

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP013")))
            .Returns(MayBe.From(transport));

        var cmd = new ChargeTransportCmd("TP013");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*non-registrasi tidak dapat diposting ke Billing*");
    }

    [Fact]
    public async Task GivenCancelledTransport_WhenHandled_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP014",
            DateTime.Now,
            regId: "REG-01",
            tujuanTransportId: "TJ-01");

        transport.Cancel("Dibatalkan pasien");

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP014")))
            .Returns(MayBe.From(transport));

        var cmd = new ChargeTransportCmd("TP014");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*telah dibatalkan tidak dapat dibebankan ke Billing*");
    }

    [Fact]
    public async Task GivenAlreadyPostedTransport_WhenHandled_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP015",
            DateTime.Now,
            regId: "REG-01",
            tujuanTransportId: "TJ-01");

        transport.SetPostedBilling();

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP015")))
            .Returns(MayBe.From(transport));

        var cmd = new ChargeTransportCmd("TP015");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dibebankan ke Billing sebelumnya*");
    }

    [Fact]
    public async Task GivenMissingRegistration_WhenHandled_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP016",
            DateTime.Now,
            regId: "REG-MISSING",
            tujuanTransportId: "TJ-01");

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP016")))
            .Returns(MayBe.From(transport));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var cmd = new ChargeTransportCmd("TP016");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*REG-MISSING*tidak ditemukan*");
    }

    [Fact]
    public async Task GivenEmptyItems_WhenChargedWithAmbulanceAndTujuan_ThenCalculatesViaTransportTariffCalculator()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP017",
            DateTime.Now,
            regId: "REG-01",
            tujuanTransportId: "TUJ01",
            konstanta: 3m,
            ambulanceId: "AMB01");

        var ambulance = new AmbulanceModel("AMB01", "Ambulance 1");
        ambulance.SetAbonement(20000m);
        ambulance.Attach([
            new AmbulanceKomponenModel("AMB01", "K-TETAP", 50000m, true),
            new AmbulanceKomponenModel("AMB01", "K-VAR", 100000m, false)
        ]);
        var tujuan = new TujuanTransportModel("TUJ01", "RS Tujuan", 3m, false);

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP017")))
            .Returns(MayBe.From(transport));
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(CreateReg("REG-01")));
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.Is<IAmbulanceKey>(k => k.AmbulanceId == "AMB01")))
            .Returns(MayBe.From(ambulance));
        _tujuanTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITujuanTransportKey>(k => k.TujuanTransportId == "TUJ01")))
            .Returns(MayBe.From(tujuan));
        _komponenRepoMock
            .Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns(Array.Empty<KomponenType>());

        var cmd = new ChargeTransportCmd("TP017");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        // K-TETAP = 50000 + 20000 (abonement), K-VAR = 100000 * 3 = 300000, Total = 370000
        result.IsPostedBilling.Should().BeTrue();
        result.Total.Should().Be(370000m);
        transport.ListItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task GivenCrewAndItemAdjustments_WhenCharged_ThenAppliesBeforePostingBilling()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP018",
            DateTime.Now,
            regId: "REG-01",
            tujuanTransportId: "TUJ01",
            konstanta: 1m,
            ambulanceId: "AMB01",
            driverId: "DRV-OLD");
        transport.AddItem(new TrsTransportItemModel("TP018", "K-TETAP", 50000m, 1m));

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP018")))
            .Returns(MayBe.From(transport));
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(CreateReg("REG-01")));
        _komponenRepoMock
            .Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns(Array.Empty<KomponenType>());

        var cmd = new ChargeTransportCmd(
            "TP018",
            DriverId: "DRV-NEW",
            PerawatId: "PRW-NEW",
            DokterId: "DOK-NEW",
            Items: [new TransportItemAdjustmentDto("K-TETAP", 60000m, 10000m)]);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        transport.DriverId.Should().Be("DRV-NEW");
        transport.PerawatId.Should().Be("PRW-NEW");
        transport.DokterId.Should().Be("DOK-NEW");
        result.Total.Should().Be(50000m);
        result.IsPostedBilling.Should().BeTrue();
        _addBillAppServiceMock.Verify(x => x.FromAmbulanceUsage(
            transport,
            It.IsAny<RegModel>(),
            It.IsAny<JaminanType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            null,
            It.IsAny<DateTime>()), Times.Once);
    }
}
