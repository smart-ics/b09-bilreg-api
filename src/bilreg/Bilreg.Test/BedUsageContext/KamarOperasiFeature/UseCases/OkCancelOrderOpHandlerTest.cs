using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkCancelOrderOpHandlerTest
{
    private readonly Mock<IOrderOpRepo> _orderOpRepo = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepo = new();
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepo = new();
    private readonly TestTglJamProvider _tglJamProvider = new(new DateTime(2026, 10, 16, 14, 0, 0));

    private OkCancelOrderOpHandler CreateSut()
    {
        return new OkCancelOrderOpHandler(
            _orderOpRepo.Object,
            _opCaseRepo.Object,
            _scheduleOpRepo.Object,
            _tglJamProvider);
    }

    private static OrderOpModel CreateSampleOrderOp(string orderOpId = "ORD-001")
    {
        var reg = RegModel.Default;
        var layanan = new LayananReff("LYN-001", "Klinik Bedah");
        var dokterPengorder = new PpaReff("DOC-ORD", "dr. Pengorder, Sp.B");
        var model = OrderOpModel.CreateByReg(reg, layanan, dokterPengorder, "user-nurse", new DateTime(2026, 10, 16, 9, 0, 0));
        model.SetProsedur("Apendektomi", "Drainase", KlasifikasiOperasiEnum.Sedang);
        return new OrderOpModel(
            orderOpId,
            model.OrderDate,
            model.AuditTrail,
            model.Pasien,
            model.Reg,
            model.Icd10,
            model.JenisOperasi,
            model.NamaOperasi,
            model.UrgencyLevel,
            model.TarifOperasi,
            model.DokterOperator,
            model.EstimasiDurasiInMinutes,
            model.PreferedDate,
            model.SpecialEquipment,
            model.DokterPengorder,
            model.LayananPengorder,
            model.ProsedurTambahan,
            model.KlasifikasiOperasi,
            model.AnestesiPlan,
            model.PostOpCareUnit,
            model.AlasanBatal,
            model.ListResource);
    }

    private static ScheduleOpModel CreateSampleSchedule(OrderOpModel orderOp, string scheduleOpId = "SCH-001")
    {
        var leader = new PpaType(
            "DOC-LEAD",
            "dr. Leader, Sp.B",
            "Sp.B",
            SmfType.Default,
            GroupSpesialisType.Default,
            [new PpaLayananType(new LayananReff("LY1", "OK"), true)],
            [new PpaSatTugasType(new SatTugasType("ST1", "Dokter", ProfesiType.Dokter), true)],
            []);

        var baseSchedule = ScheduleOpModel.CreateFromOrder(
            orderOp,
            "user-sch",
            KamarType.Default,
            leader,
            DateTime.Today.AddDays(1),
            new DateTime(2026, 10, 16, 10, 0, 0));

        return new ScheduleOpModel(
            scheduleOpId,
            baseSchedule.ScheduleOpDate,
            baseSchedule.AuditTrail,
            baseSchedule.OrderOp,
            baseSchedule.Pasien,
            baseSchedule.UrgencyLevel,
            baseSchedule.Durasi,
            baseSchedule.TglOp,
            baseSchedule.KamarOp,
            baseSchedule.Reg,
            baseSchedule.TeamLead,
            baseSchedule.StartOpDate,
            baseSchedule.EndOpDate,
            baseSchedule.OrderOpState,
            baseSchedule.ListPpa);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenAlasanBatalNullOrWhitespace_ShouldThrowArgumentException(string? alasanBatal)
    {
        // Act
        var act = () => new OkCancelOrderOpCommand("ORD-001", alasanBatal!, "USER-001");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenOrderOpIdNullOrWhitespace_ShouldThrowArgumentException(string? orderOpId)
    {
        // Act
        var act = () => new OkCancelOrderOpCommand(orderOpId!, "Kondisi pasien tidak memungkinkan", "USER-001");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenOrderOpNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _orderOpRepo.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe<OrderOpModel>.None);
        var sut = CreateSut();
        var cmd = new OkCancelOrderOpCommand("ORD-MISSING", "Batal karena pasien menolak", "USER-001");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenOpCaseNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001");
        _orderOpRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepo.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe<OpCaseModel>.None);

        var sut = CreateSut();
        var cmd = new OkCancelOrderOpCommand("ORD-001", "Batal karena pasien menolak", "USER-001");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenOpCaseStateIsOpStarted_ShouldThrowInvalidOperationException_EX05()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 9, 0, 0));
        var schedule = CreateSampleSchedule(orderOp);
        opCase.Schedule(schedule, new DateTime(2026, 10, 16, 10, 0, 0));
        opCase.Start(new DateTime(2026, 10, 16, 11, 0, 0));

        _orderOpRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));

        var sut = CreateSut();
        var cmd = new OkCancelOrderOpCommand("ORD-001", "Batal di tengah jalan", "USER-001");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak dapat dibatalkan*");
    }

    [Fact]
    public async Task Handle_WhenOpCaseStateIsRecoveryStarted_ShouldThrowInvalidOperationException_EX05()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 9, 0, 0));
        var schedule = CreateSampleSchedule(orderOp);
        opCase.Schedule(schedule, new DateTime(2026, 10, 16, 10, 0, 0));
        opCase.Start(new DateTime(2026, 10, 16, 11, 0, 0));
        opCase.Finish(new DateTime(2026, 10, 16, 13, 0, 0));

        _orderOpRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));

        var sut = CreateSut();
        var cmd = new OkCancelOrderOpCommand("ORD-001", "Batal di recovery", "USER-001");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak dapat dibatalkan*");
    }

    [Fact]
    public async Task Handle_WhenOpCaseStateIsDischarged_ShouldThrowInvalidOperationException_EX05()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 9, 0, 0));
        var schedule = CreateSampleSchedule(orderOp);
        opCase.Schedule(schedule, new DateTime(2026, 10, 16, 10, 0, 0));
        opCase.Start(new DateTime(2026, 10, 16, 11, 0, 0));
        opCase.Finish(new DateTime(2026, 10, 16, 13, 0, 0));
        var discharge = DischargeOpModel.Create(
            orderOp,
            new DateTime(2026, 10, 16, 13, 30, 0),
            KamarType.Default,
            PatientConditionEnum.Stable,
            "Kondisi baik, pindah bangsal",
            "USER-DIS");
        opCase.Discharge(discharge, new DateTime(2026, 10, 16, 13, 30, 0));

        _orderOpRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));

        var sut = CreateSut();
        var cmd = new OkCancelOrderOpCommand("ORD-001", "Batal setelah discharge", "USER-001");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak dapat dibatalkan*");
    }

    [Fact]
    public async Task Handle_WhenStatusIsRequested_ShouldCancelOrderAndOpCase_WithoutScheduleCancellation()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 9, 0, 0));

        _orderOpRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));

        var sut = CreateSut();
        var cmd = new OkCancelOrderOpCommand("ORD-001", "Permintaan dibatalkan oleh pasien", "USER-CANCEL");

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderOpId.Should().Be("ORD-001");
        result.Status.Should().Be(OpCaseStateEnum.Cancelled.ToString());
        result.AlasanBatal.Should().Be("Permintaan dibatalkan oleh pasien");

        orderOp.AlasanBatal.Should().Be("Permintaan dibatalkan oleh pasien");
        orderOp.AuditTrail.IsVoided.Should().BeTrue();
        orderOp.AuditTrail.Voided.UserId.Should().Be("USER-CANCEL");
        orderOp.AuditTrail.Voided.Timestamp.Should().Be(_tglJamProvider.Now);

        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Cancelled);

        _orderOpRepo.Verify(x => x.SaveChanges(orderOp), Times.Once);
        _opCaseRepo.Verify(x => x.SaveChanges(opCase), Times.Once);
        _scheduleOpRepo.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStatusIsScheduled_WithScheduleOpId_ShouldCancelScheduleAndOrderAndOpCase()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001");
        var schedule = CreateSampleSchedule(orderOp, "SCH-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 9, 0, 0));
        opCase.Schedule(schedule, new DateTime(2026, 10, 16, 10, 0, 0));

        _orderOpRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepo.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(schedule));

        var sut = CreateSut();
        var cmd = new OkCancelOrderOpCommand("ORD-001", "Kondisi hemodinamik pasien tidak stabil", "USER-CANCEL");

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderOpId.Should().Be("ORD-001");
        result.Status.Should().Be(OpCaseStateEnum.Cancelled.ToString());
        result.AlasanBatal.Should().Be("Kondisi hemodinamik pasien tidak stabil");

        orderOp.AlasanBatal.Should().Be("Kondisi hemodinamik pasien tidak stabil");
        orderOp.AuditTrail.IsVoided.Should().BeTrue();
        orderOp.AuditTrail.Voided.UserId.Should().Be("USER-CANCEL");

        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Cancelled);
        schedule.AuditTrail.IsVoided.Should().BeTrue();
        schedule.AuditTrail.Voided.UserId.Should().Be("USER-CANCEL");
        schedule.AuditTrail.Voided.Timestamp.Should().Be(_tglJamProvider.Now);

        _orderOpRepo.Verify(x => x.SaveChanges(orderOp), Times.Once);
        _opCaseRepo.Verify(x => x.SaveChanges(opCase), Times.Once);
        _scheduleOpRepo.Verify(x => x.SaveChanges(schedule), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStatusIsScheduled_FallbackToPatientScheduleList_ShouldCancelSchedule()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001");
        var schedule = CreateSampleSchedule(orderOp, "SCH-999");
        // Create OpCase in Scheduled state but with ScheduleOpReff.Default ("-")
        var opCase = new OpCaseModel(
            orderOp.OrderOpId,
            orderOp.ToReff(),
            orderOp.Pasien,
            orderOp.NamaOperasi,
            orderOp.Reg,
            orderOp.UrgencyLevel,
            ScheduleOpReff.Default,
            DischargeOpReff.Default,
            DuranteOpReff.Default,
            OpCaseStateEnum.Scheduled,
            [new OpCaseStateHistType(0, OpCaseStateEnum.Scheduled, DateTime.Now)],
            []);

        _orderOpRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));

        var scheduleView = new ScheduleOpView(
            "SCH-999",
            orderOp.Pasien,
            orderOp.ToReff(),
            orderOp.UrgencyLevel,
            DateTime.Today.AddDays(1),
            60,
            PpaReff.Default,
            KamarType.Default.ToReff(),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepo.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == orderOp.Pasien.PasienId)))
            .Returns([scheduleView]);
        _scheduleOpRepo.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-999")))
            .Returns(MayBe.From(schedule));

        var sut = CreateSut();
        var cmd = new OkCancelOrderOpCommand("ORD-001", "Pembatalan dengan fallback list schedule", "USER-FALLBACK");

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OpCaseStateEnum.Cancelled.ToString());
        schedule.AuditTrail.IsVoided.Should().BeTrue();
        schedule.AuditTrail.Voided.UserId.Should().Be("USER-FALLBACK");

        _scheduleOpRepo.Verify(x => x.SaveChanges(schedule), Times.Once);
        _orderOpRepo.Verify(x => x.SaveChanges(orderOp), Times.Once);
        _opCaseRepo.Verify(x => x.SaveChanges(opCase), Times.Once);
    }
}
