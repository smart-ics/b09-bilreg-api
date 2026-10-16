using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Domain.Shared.Param;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class SaveBridgeOpOnScheduleOpSetEvHandlerTest
{
    private readonly Mock<IGetProjectIdService> _projectIdSvcMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IBridgeOperasiSaveService> _bridgeServiceMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();

    private SaveBridgeOpOnScheduleOpSetEvHandler CreateSut()
    {
        return new SaveBridgeOpOnScheduleOpSetEvHandler(
            _projectIdSvcMock.Object,
            _orderOpRepoMock.Object,
            _bridgeServiceMock.Object,
            _regRepoMock.Object);
    }

    private static RegModel CreateSampleReg(string regId = "REG-001")
    {
        return new RegModel(
            regId,
            new DateOnly(2026, 10, 16),
            new AuditInfoType("user-reg", new DateTime(2026, 10, 16)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            new PasienReff("PAS-001", "Pasien BPJS", new DateOnly(1990, 1, 1), "L"),
            new TipeJaminanReff("BPJS", "BPJS Kesehatan"),
            new PolisReff("POL-01", "0001234567890", "Pasien BPJS"),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            new PpaReff("DOC-01", "dr. Bedah"),
            new LayananReff("LYN-OK", "Kamar Bedah Sentral"),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);
    }

    private static OrderOpModel CreateSampleOrderOp(string orderOpId = "ORD-001", string? tarifName = null, string namaOperasi = "Herniotomi")
    {
        var tarif = string.IsNullOrEmpty(tarifName) ? TarifReff.Default : new TarifReff("TRF-01", tarifName);
        return new OrderOpModel(
            orderOpId,
            new DateTime(2026, 10, 16),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 16)),
            new PasienReff("PAS-001", "Pasien BPJS", new DateOnly(1990, 1, 1), "L"),
            new RegReff("REG-001", "PAS-001", "Pasien BPJS"),
            new Icd10Type("K40", "Hernia"),
            new JenisOperasiType("JOP-1", "Bedah"),
            namaOperasi,
            UrgencyLevelEnum.Elective,
            tarif,
            new PpaReff("DOC-01", "dr. Bedah"),
            60,
            new DateTime(2026, 10, 20),
            "-",
            new PpaReff("DOC-01", "dr. Bedah"),
            new LayananReff("LYN-01", "Klinik Bedah"),
            "",
            KlasifikasiOperasiEnum.Sedang,
            AnestesiPlanEnum.RegionalSpinal,
            PostOpCareUnitEnum.PACU,
            "",
            []);
    }

    private static OkScheduleOpSetEvent CreateSampleEvent(string orderOpId = "ORD-001", string regId = "REG-001")
    {
        var cmd = new OkScheduleOpSetCommand(orderOpId, "KAMAR-1", "2026-10-20", "08:00", 60, "u1");
        var schedule = new ScheduleOpModel(
            "SCH-001",
            new DateTime(2026, 10, 16),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 16)),
            new OrderOpReff(orderOpId, new DateTime(2026, 10, 16), "Herniotomi"),
            new PasienReff("PAS-001", "Pasien BPJS", new DateOnly(1990, 1, 1), "L"),
            UrgencyLevelEnum.Elective,
            60,
            new DateTime(2026, 10, 20),
            new KamarReff("KAMAR-1", "OK 1"),
            new RegReff(regId, "PAS-001", "Pasien BPJS"),
            new PpaReff("DOC-01", "dr. Bedah"),
            new DateTime(2026, 10, 20, 8, 0, 0),
            new DateTime(2026, 10, 20, 9, 0, 0),
            OpCaseStateEnum.Scheduled,
            []);

        return new OkScheduleOpSetEvent(cmd, schedule);
    }

    [Fact]
    public async Task Handle_WhenOrderOpHasTarifName_ShouldCallBridgeServiceWithTarifName()
    {
        // Arrange
        _projectIdSvcMock.Setup(x => x.Execute()).Returns("RS001");
        var orderOp = CreateSampleOrderOp("ORD-001", tarifName: "Tarif Herniotomi Elektif", namaOperasi: "Herniotomi");
        var reg = CreateSampleReg("REG-001");
        var ev = CreateSampleEvent("ORD-001", "REG-001");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-001")))
            .Returns(MayBe.From(reg));

        BridgeOperasiCmd? capturedCmd = null;
        _bridgeServiceMock.Setup(x => x.Execute(It.IsAny<BridgeOperasiCmd>()))
            .Callback<BridgeOperasiCmd>(c => capturedCmd = c);

        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        capturedCmd.Should().NotBeNull();
        capturedCmd!.JenisTindakan.Should().Be("Tarif Herniotomi Elektif");
        capturedCmd.RsId.Should().Be("RS001");
        capturedCmd.KodeBooking.Should().Be("REG-001");
        capturedCmd.NoPeserta.Should().Be("0001234567890");
        _bridgeServiceMock.Verify(x => x.Execute(It.IsAny<BridgeOperasiCmd>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderOpHasNoTarifOrEmptyTarif_ShouldFallbackToNamaOperasi()
    {
        // Arrange
        _projectIdSvcMock.Setup(x => x.Execute()).Returns("RS001");
        var orderOp = CreateSampleOrderOp("ORD-001", tarifName: null, namaOperasi: "Herniotomi Prosedur Utama");
        var reg = CreateSampleReg("REG-001");
        var ev = CreateSampleEvent("ORD-001", "REG-001");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-001")))
            .Returns(MayBe.From(reg));

        BridgeOperasiCmd? capturedCmd = null;
        _bridgeServiceMock.Setup(x => x.Execute(It.IsAny<BridgeOperasiCmd>()))
            .Callback<BridgeOperasiCmd>(c => capturedCmd = c);

        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        capturedCmd.Should().NotBeNull();
        capturedCmd!.JenisTindakan.Should().Be("Herniotomi Prosedur Utama");
        _bridgeServiceMock.Verify(x => x.Execute(It.IsAny<BridgeOperasiCmd>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderOpNotFound_ShouldNotCallBridgeService()
    {
        // Arrange
        _projectIdSvcMock.Setup(x => x.Execute()).Returns("RS001");
        var ev = CreateSampleEvent("ORD-NOTFOUND", "REG-001");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe<OrderOpModel>.None);

        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        _bridgeServiceMock.Verify(x => x.Execute(It.IsAny<BridgeOperasiCmd>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRegNotFound_ShouldNotCallBridgeService()
    {
        // Arrange
        _projectIdSvcMock.Setup(x => x.Execute()).Returns("RS001");
        var orderOp = CreateSampleOrderOp("ORD-001");
        var ev = CreateSampleEvent("ORD-001", "REG-NOTFOUND");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(orderOp));
        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        _bridgeServiceMock.Verify(x => x.Execute(It.IsAny<BridgeOperasiCmd>()), Times.Never);
    }
}
