using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkScheduleOpCancelHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private OkScheduleOpCancelHandler CreateSut()
    {
        return new OkScheduleOpCancelHandler(
            _scheduleOpRepoMock.Object,
            _orderOpRepoMock.Object,
            _opCaseRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    private static OrderOpModel CreateSampleOrderOp(string orderOpId = "ORD-001")
    {
        return new OrderOpModel(
            orderOpId,
            new DateTime(2026, 10, 20),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 20)),
            new PasienReff("PAS-001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            new RegReff("REG-001", "PAS-001", "Pasien A"),
            new Icd10Type("K40", "Hernia"),
            new JenisOperasiType("JOP-1", "Bedah Umum"),
            "Herniotomi",
            UrgencyLevelEnum.Elective,
            TarifReff.Default,
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            60,
            new DateTime(2026, 10, 20),
            "-",
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            new LayananReff("LYN-01", "Klinik Bedah"),
            "",
            KlasifikasiOperasiEnum.Sedang,
            AnestesiPlanEnum.General,
            PostOpCareUnitEnum.PACU,
            "",
            []);
    }

    private static ScheduleOpModel CreateSampleScheduleOp(string scheduleOpId = "SCH-001", string orderOpId = "ORD-001")
    {
        var opTime = new DateTime(2026, 10, 20, 8, 0, 0);
        return new ScheduleOpModel(
            scheduleOpId,
            new DateTime(2026, 10, 20),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 20)),
            new OrderOpReff(orderOpId, new DateTime(2026, 10, 20), "Herniotomi"),
            new PasienReff("PAS-001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            UrgencyLevelEnum.Elective,
            60,
            opTime,
            new KamarReff("KAMAR-1", "OK 1"),
            new RegReff("REG-001", "PAS-001", "Pasien A"),
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            opTime,
            opTime.AddMinutes(60),
            OpCaseStateEnum.Scheduled,
            [new ScheduleOpPpaType(1, new PpaReff("DOC-01", "dr. Bedah Sp.B"), ProfesiType.Dokter, GroupSpesialisType.Default)],
            "",
            30,
            []);
    }

    [Fact]
    public async Task Handle_CancelSchedule_Success()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 9, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001");
        opCase.Schedule(scheduleOp, new DateTime(2026, 10, 20, 7, 0, 0));

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        var cmd = new OkScheduleOpCancelCommand("ORD-001", "user1", "Pasien demam tinggi dan tidak stabil");
        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        scheduleOp.AlasanBatal.Should().Be("Pasien demam tinggi dan tidak stabil");
        scheduleOp.AuditTrail.IsVoided.Should().BeTrue();
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Requested);

        _scheduleOpRepoMock.Verify(x => x.SaveChanges(scheduleOp), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(opCase), Times.Once);
    }

    [Fact]
    public async Task Handle_CancelSchedule_WithoutAlasanBatal_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new OkScheduleOpCancelCommand("ORD-001", "user1", "");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Alasan pembatalan jadwal operasi wajib diisi.*");
    }

    [Fact]
    public async Task Handle_CancelSchedule_ScheduleNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView>());

        var cmd = new OkScheduleOpCancelCommand("ORD-001", "user1", "Alasan valid");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Schedule Operasi tidak ditemukan.");
    }

    [Fact]
    public async Task Handle_CancelSchedule_PreservesOrderOpDokterOperatorWithoutReversion()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 9, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001");
        // Simulate mutated doctor operator from AssignLeader (e.g. DR00139)
        orderOp.SetDokterOperator(new PpaReff("DR00139", "dr. Arya Yunan, Sp.B"));

        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001");
        opCase.Schedule(scheduleOp, new DateTime(2026, 10, 20, 7, 0, 0));

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        var cmd = new OkScheduleOpCancelCommand("ORD-001", "user1", "Pasien demam tinggi dan operasi ditunda");
        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Requested);
        orderOp.DokterOperator.PpaId.Should().Be("DR00139");
        orderOp.DokterOperator.PpaName.Should().Be("dr. Arya Yunan, Sp.B");

        // OrderOp should not be re-saved or reverted upon cancel
        _orderOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderOpModel>()), Times.Never);
    }
}
