using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
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
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class LinkRegTransportCmdTest
{
    private readonly Mock<ITrsTransportRepo> _trsTransportRepoMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly LinkRegTransportHandler _sut;

    public LinkRegTransportCmdTest()
    {
        _sut = new LinkRegTransportHandler(_trsTransportRepoMock.Object, _regRepoMock.Object);
    }

    private static RegModel CreateReg(string regId, bool isAktif = true)
    {
        var masukAudit = new AuditInfoType("USER1", new DateTime(2026, 11, 16, 8, 0, 0));
        var keluarAudit = isAktif ? AuditInfoType.Default : new AuditInfoType("USER1", new DateTime(2026, 11, 16, 9, 0, 0));
        var tipeJaminan = new TipeJaminanReff("J0011", "Jaminan Test");

        return new RegModel(
            regId,
            new DateOnly(2026, 11, 16),
            masukAudit,
            keluarAudit,
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
    public async Task GivenValidNonRegTransport_WhenLinked_ThenUpdatesRegIdAndIsNonRegFalse()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.InboundRetrieval,
            ChargingMethodEnum.Jarak,
            "IGD",
            "USER1",
            "TP001",
            DateTime.Now);

        transport.IsNonReg.Should().BeTrue();

        var reg = CreateReg("REG-001", isAktif: true);

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP001")))
            .Returns(MayBe.From(transport));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-001")))
            .Returns(MayBe.From(reg));

        var cmd = new LinkRegTransportCmd("TP001", "REG-001");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TrsTransportId.Should().Be("TP001");
        result.RegId.Should().Be("REG-001");

        transport.RegId.Should().Be("REG-001");
        transport.IsNonReg.Should().BeFalse();
        _trsTransportRepoMock.Verify(x => x.SaveChanges(transport), Times.Once);
    }

    [Fact]
    public async Task GivenInactiveRegistration_WhenLinked_ThenThrowsArgumentException()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.InboundRetrieval,
            ChargingMethodEnum.Jarak,
            "IGD",
            "USER1",
            "TP002",
            DateTime.Now);

        var reg = CreateReg("REG-002", isAktif: false);

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP002")))
            .Returns(MayBe.From(transport));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-002")))
            .Returns(MayBe.From(reg));

        var cmd = new LinkRegTransportCmd("TP002", "REG-002");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak aktif*");
    }

    [Fact]
    public async Task GivenRegistrationNotFound_WhenLinked_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        var transport = TrsTransportModel.Create(
            TransportTypeEnum.InboundRetrieval,
            ChargingMethodEnum.Jarak,
            "IGD",
            "USER1",
            "TP003",
            DateTime.Now);

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP003")))
            .Returns(MayBe.From(transport));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-NONEXIST")))
            .Returns(MayBe<RegModel>.None);

        var cmd = new LinkRegTransportCmd("TP003", "REG-NONEXIST");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*REG-NONEXIST*");
    }
}
