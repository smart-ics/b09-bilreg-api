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
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class CompleteTransportCmdTest
{
    private readonly Mock<ITrsTransportRepo> _trsTransportRepoMock = new();
    private readonly Mock<IAmbulanceRepo> _ambulanceRepoMock = new();
    private readonly Mock<IAddBillAppService> _addBillAppServiceMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IJaminanRepo> _jaminanRepoMock = new();
    private readonly Mock<IKomponenRepo> _komponenRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly CompleteTransportHandler _sut;

    public CompleteTransportCmdTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 11, 16, 12, 0, 0));
        _sut = new CompleteTransportHandler(
            _trsTransportRepoMock.Object,
            _ambulanceRepoMock.Object,
            _addBillAppServiceMock.Object,
            _regRepoMock.Object,
            _jaminanRepoMock.Object,
            _komponenRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    private static TrsTransportModel CreateDispatchedTransport(string id = "TP010", string regId = "REG-01", decimal startKm = 1000m)
    {
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            id,
            DateTime.Now,
            regId: regId,
            tujuanTransportId: "TJ-01",
            konstanta: 150000m);

        transport.Dispatch("AMB-01", "DRV-01", "B 1234 CD", startKm, dispatchTime: DateTime.Now.AddHours(-1));
        return transport;
    }

    private static RegModel CreateReg(string regId)
    {
        var masukAudit = new AuditInfoType("USER1", new DateTime(2026, 11, 16, 8, 0, 0));
        var tipeJaminan = new TipeJaminanReff("BPJS", "BPJS Kesehatan");

        return new RegModel(
            regId,
            new DateOnly(2026, 11, 16),
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
    public async Task GivenRegisteredTransport_WhenCompleted_ThenPostsToBillingAndSetsPostedBilling()
    {
        // Arrange
        var transport = CreateDispatchedTransport("TP010", regId: "REG-01", startKm: 1000m);
        var reg = CreateReg("REG-01");

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP010")))
            .Returns(MayBe.From(transport));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));

        var jaminan = JaminanType.Default with { JaminanId = "BPJS", JaminanName = "BPJS Kesehatan" };
        _jaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(jaminan));

        _komponenRepoMock
            .Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([]);

        var items = new List<TrsTransportItemInputDto>
        {
            new("KOMP-01", 150000m, 1m, Keterangan: "Tarif Wilayah"),
            new("KOMP-02", 50000m, 1m, Keterangan: "Jasa Pendamping")
        };

        var cmd = new CompleteTransportCmd("TP010", 1045m, Items: items);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Completed");
        result.Total.Should().Be(200000m);
        result.IsPostedBilling.Should().BeTrue();

        transport.Status.Should().Be(TransportStatusEnum.Completed);
        transport.EndOdometer.Should().Be(1045m);
        transport.TotalJarakKm.Should().Be(45m);
        transport.IsPostedBilling.Should().BeTrue();

        _addBillAppServiceMock.Verify(x => x.FromAmbulanceUsage(
            transport,
            reg,
            It.IsAny<JaminanType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            null,
            It.IsAny<DateTime>()), Times.Once);

        _trsTransportRepoMock.Verify(x => x.SaveChanges(transport), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GivenNonRegTransport_WhenCompleted_ThenDoesNotPostToBilling()
    {
        // Arrange
        var transport = CreateDispatchedTransport("TP011", regId: "", startKm: 500m);
        transport.IsNonReg.Should().BeTrue();

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP011")))
            .Returns(MayBe.From(transport));

        var items = new List<TrsTransportItemInputDto>
        {
            new("KOMP-01", 120000m, 1m)
        };

        var cmd = new CompleteTransportCmd("TP011", 530m, Items: items);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Completed");
        result.Total.Should().Be(120000m);
        result.IsPostedBilling.Should().BeFalse();

        transport.Status.Should().Be(TransportStatusEnum.Completed);
        transport.IsPostedBilling.Should().BeFalse();

        _addBillAppServiceMock.Verify(x => x.FromAmbulanceUsage(
            It.IsAny<TrsTransportModel>(),
            It.IsAny<RegModel>(),
            It.IsAny<JaminanType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            null,
            It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GivenEndOdometerLessThanStartOdometer_WhenCompleted_ThenThrowsArgumentException()
    {
        // Arrange
        var transport = CreateDispatchedTransport("TP012", regId: "REG-01", startKm: 1000m);

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP012")))
            .Returns(MayBe.From(transport));

        var cmd = new CompleteTransportCmd("TP012", 950m); // Lower than 1000m

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*End odometer*tidak boleh lebih kecil*");
    }

    [Fact]
    public async Task GivenTransportNotInDispatchedState_WhenCompleted_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP013",
            DateTime.Now,
            tujuanTransportId: "TJ-01");

        transport.Status.Should().Be(TransportStatusEnum.Requested);

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP013")))
            .Returns(MayBe.From(transport));

        var cmd = new CompleteTransportCmd("TP013", 100m);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Transisi hanya valid dari status Dispatched*");
    }

    [Fact]
    public async Task GivenExistingEstimatedItem_WhenCompletedWithExplicitItems_ThenReplacesItemsWithoutDuplication()
    {
        // Arrange
        var transport = CreateDispatchedTransport("TP014", regId: "");
        transport.AddItem(new TrsTransportItemModel("TP014", "KOMP-EST", 150000m, 1m));

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP014")))
            .Returns(MayBe.From(transport));

        var cmd = new CompleteTransportCmd(
            "TP014",
            1050m,
            Items: new[] { new TrsTransportItemInputDto("KOMP-ACT", 200000m, 1m) });

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Total.Should().Be(200000m);
        transport.ListItems.Should().HaveCount(1);
        transport.ListItems.First().DetilTarifId.Should().Be("KOMP-ACT");
    }
}
