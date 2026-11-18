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
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class CancelTransportCmdTest
{
    private readonly Mock<ITrsTransportRepo> _trsTransportRepoMock = new();
    private readonly Mock<IAmbulanceRepo> _ambulanceRepoMock = new();
    private readonly Mock<IAddBillAppService> _addBillAppServiceMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IJaminanRepo> _jaminanRepoMock = new();
    private readonly Mock<IKomponenRepo> _komponenRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly CancelTransportHandler _sut;

    public CancelTransportCmdTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 11, 16, 14, 0, 0));
        _sut = new CancelTransportHandler(
            _trsTransportRepoMock.Object,
            _ambulanceRepoMock.Object,
            _addBillAppServiceMock.Object,
            _regRepoMock.Object,
            _jaminanRepoMock.Object,
            _komponenRepoMock.Object,
            _tglJamProviderMock.Object);
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
    public async Task GivenRequestedTransport_WhenCancelled_ThenZeroFeeAndNoBillingPosting()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP020",
            DateTime.Now,
            regId: "REG-01",
            tujuanTransportId: "TJ-01");

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP020")))
            .Returns(MayBe.From(transport));

        var cmd = new CancelTransportCmd("TP020", "Pasien membatalkan sebelum armada jalan");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Cancelled");
        result.BiayaBatal.Should().Be(0m);
        result.IsPostedBilling.Should().BeFalse();

        transport.Status.Should().Be(TransportStatusEnum.Cancelled);
        transport.AlasanBatal.Should().Be("Pasien membatalkan sebelum armada jalan");
        transport.BiayaBatal.Should().Be(0m);
        transport.IsVoid.Should().BeTrue();

        _addBillAppServiceMock.Verify(x => x.FromAmbulanceUsage(
            It.IsAny<TrsTransportModel>(),
            It.IsAny<RegModel>(),
            It.IsAny<JaminanType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            null,
            It.IsAny<DateTime>()), Times.Never);

        _trsTransportRepoMock.Verify(x => x.SaveChanges(transport), Times.Once);
    }

    [Fact]
    public async Task GivenDispatchedTransportWithReg_WhenCancelled_ThenPostsStandbyFeeToBilling()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP021",
            DateTime.Now,
            regId: "REG-01",
            tujuanTransportId: "TJ-01");

        transport.Dispatch("AMB-01", "DRV-01", "B 1234 CD", 500m);

        var reg = CreateReg("REG-01");
        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP021")))
            .Returns(MayBe.From(transport));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));

        _jaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(JaminanType.Default));

        var cmd = new CancelTransportCmd("TP021", "Dibatalkan keluarga di jalan", BiayaBatal: 75000m);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Cancelled");
        result.BiayaBatal.Should().Be(75000m);
        result.IsPostedBilling.Should().BeTrue();

        transport.Status.Should().Be(TransportStatusEnum.Cancelled);
        transport.BiayaBatal.Should().Be(75000m);
        transport.IsPostedBilling.Should().BeTrue();

        _addBillAppServiceMock.Verify(x => x.FromAmbulanceUsage(
            transport,
            reg,
            It.IsAny<JaminanType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            null,
            It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task GivenDispatchedTransportWithoutExplicitFee_WhenCancelled_ThenResolvesAbonementFromAmbulance()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP022",
            DateTime.Now,
            regId: "REG-01",
            tujuanTransportId: "TJ-01");

        transport.Dispatch("AMB-01", "DRV-01", "B 1234 CD", 500m);

        var reg = CreateReg("REG-01");
        var ambulance = new AmbulanceModel("AMB-01", "Ambulance 1");
        ambulance.SetAbonement(60000m);

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP022")))
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

        var cmd = new CancelTransportCmd("TP022", "Dibatalkan setelah dispatch");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.BiayaBatal.Should().Be(60000m);
        result.IsPostedBilling.Should().BeTrue();
        transport.BiayaBatal.Should().Be(60000m);
    }

    [Fact]
    public async Task GivenDispatchedNonRegTransport_WhenCancelled_ThenDoesNotPostToBilling()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP023",
            DateTime.Now,
            regId: "",
            tujuanTransportId: "TJ-01");

        transport.Dispatch("AMB-01", "DRV-01", "B 1234 CD", 500m);
        transport.IsNonReg.Should().BeTrue();

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP023")))
            .Returns(MayBe.From(transport));

        var cmd = new CancelTransportCmd("TP023", "Non-reg cancel", BiayaBatal: 50000m);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.BiayaBatal.Should().Be(50000m);
        result.IsPostedBilling.Should().BeFalse();

        _addBillAppServiceMock.Verify(x => x.FromAmbulanceUsage(
            It.IsAny<TrsTransportModel>(),
            It.IsAny<RegModel>(),
            It.IsAny<JaminanType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            null,
            It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GivenCompletedTransport_WhenCancelled_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP024",
            DateTime.Now,
            tujuanTransportId: "TJ-01");

        transport.Dispatch("AMB-01", "DRV-01", "B 1234 CD", 500m);
        transport.Complete(520m);

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP024")))
            .Returns(MayBe.From(transport));

        var cmd = new CancelTransportCmd("TP024", "Membatalkan completed");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Completed tidak dapat dibatalkan langsung*");
    }
}
