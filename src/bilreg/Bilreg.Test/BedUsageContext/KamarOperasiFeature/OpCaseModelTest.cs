using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OpCaseModelTest
{
    private static OrderOpModel CreateSampleOrderOp()
    {
        var reg = RegModel.Default;
        var layanan = new LayananReff("LYN-001", "Klinik Bedah");
        var dokterPengorder = new PpaReff("DOC-ORD", "dr. Pengorder, Sp.B");
        var sut = OrderOpModel.CreateByReg(reg, layanan, dokterPengorder, "user-nurse", new DateTime(2026, 10, 16, 9, 0, 0));
        sut.SetProsedur("Apendektomi", "Drainase", KlasifikasiOperasiEnum.Sedang);
        return sut;
    }

    private static ScheduleOpModel CreateSampleSchedule(OrderOpModel orderOp)
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

        return ScheduleOpModel.CreateFromOrder(
            orderOp,
            "user-sch",
            KamarType.Default,
            leader,
            DateTime.Today.AddDays(1),
            new DateTime(2026, 10, 16, 10, 0, 0));
    }

    [Fact]
    public void Create_FromOrderOp_ShouldInstantiateWithRequestedStateAndRequesterPpa()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var createdAt = new DateTime(2026, 10, 16, 9, 5, 0);

        // Act
        var sut = OpCaseModel.Create(orderOp, createdAt);

        // Assert
        sut.Should().NotBeNull();
        sut.OrderOpId.Should().Be(orderOp.OrderOpId);
        sut.OrderOpState.Should().Be(OpCaseStateEnum.Requested);
        sut.ScheduleOp.Should().Be(ScheduleOpReff.Default);
        sut.DuranteOp.Should().Be(DuranteOpReff.Default);

        sut.ListStateHistory.Should().HaveCount(1);
        var initialHist = sut.ListStateHistory.First();
        initialHist.OpCaseState.Should().Be(OpCaseStateEnum.Requested);
        initialHist.StateTimestamp.Should().Be(createdAt);

        sut.ListPpa.Should().HaveCount(1);
        var requesterPpa = sut.ListPpa.First();
        requesterPpa.Role.Should().Be("REQUESTER");
        requesterPpa.Ppa.Should().Be(orderOp.DokterPengorder);
    }

    [Fact]
    public void ActiveOpCase_WhenCancelled_ShouldReturnNull()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp);

        // Act
        sut.CancelOrder(DateTime.Now);

        // Assert
        sut.OrderOpState.Should().Be(OpCaseStateEnum.Cancelled);
        sut.ActiveOpCase.Should().BeNull();
    }

    [Fact]
    public void ActiveOpCase_WhenRequested_ShouldReturnActiveCase()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp);

        // Assert
        sut.ActiveOpCase.Should().NotBeNull();
        sut.ActiveOpCase!.OpCaseState.Should().Be(OpCaseStateEnum.Requested);
    }

    [Fact]
    public void CancelOrder_FromRequested_ShouldTransitionToCancelledAndRecordHistory()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 8, 0, 0));
        var cancelledAt = new DateTime(2026, 10, 16, 10, 30, 0);

        // Act
        sut.CancelOrder(cancelledAt);

        // Assert
        sut.OrderOpState.Should().Be(OpCaseStateEnum.Cancelled);
        sut.ScheduleOp.Should().Be(ScheduleOpReff.Default);
        sut.DuranteOp.Should().Be(DuranteOpReff.Default);

        sut.ListStateHistory.Should().Contain(x =>
            x.OpCaseState == OpCaseStateEnum.Cancelled && x.StateTimestamp == cancelledAt);
    }

    [Fact]
    public void CancelOrder_FromScheduled_ShouldTransitionToCancelledAndResetSchedule()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 8, 0, 0));
        var schedule = CreateSampleSchedule(orderOp);
        sut.Schedule(schedule, new DateTime(2026, 10, 16, 9, 0, 0));
        sut.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);

        var cancelledAt = new DateTime(2026, 10, 16, 11, 0, 0);

        // Act
        sut.CancelOrder(cancelledAt);

        // Assert
        sut.OrderOpState.Should().Be(OpCaseStateEnum.Cancelled);
        sut.ScheduleOp.Should().Be(ScheduleOpReff.Default);
        sut.ListStateHistory.Should().Contain(x =>
            x.OpCaseState == OpCaseStateEnum.Cancelled && x.StateTimestamp == cancelledAt);
    }

    [Fact]
    public void CancelOrder_WhenOpStarted_ShouldThrowInvalidOperationException()
    {
        // Arrange (EX-05: Operasi sudah dimulai atau selesai tidak dapat dibatalkan)
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp);
        var schedule = CreateSampleSchedule(orderOp);
        sut.Schedule(schedule);
        sut.Start(DateTime.Now);
        sut.OrderOpState.Should().Be(OpCaseStateEnum.OpStarted);

        // Act & Assert
        FluentActions.Invoking(() => sut.CancelOrder(DateTime.Now))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Status Operasi tidak dapat dibatalkan*");
    }

    [Fact]
    public void CancelOrder_WhenRecoveryStarted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp);
        var schedule = CreateSampleSchedule(orderOp);
        sut.Schedule(schedule);
        sut.Start(DateTime.Now.AddHours(-2));
        sut.Finish(DateTime.Now.AddHours(-1));
        sut.OrderOpState.Should().Be(OpCaseStateEnum.RecoveryStarted);

        // Act & Assert
        FluentActions.Invoking(() => sut.CancelOrder(DateTime.Now))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Status Operasi tidak dapat dibatalkan*");
    }

    [Fact]
    public void CancelOrder_WhenDischarged_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp);
        var schedule = CreateSampleSchedule(orderOp);
        sut.Schedule(schedule);
        sut.Start(DateTime.Now.AddHours(-3));
        sut.Finish(DateTime.Now.AddHours(-2));
        var discharge = DischargeOpModel.Create(
            orderOp,
            DateTime.Now.AddHours(-1),
            KamarType.Default,
            PatientConditionEnum.Stable,
            "Kondisi baik, pindah bangsal",
            "user-doc");
        sut.Discharge(discharge, DateTime.Now.AddHours(-1));
        sut.OrderOpState.Should().Be(OpCaseStateEnum.Discharged);

        // Act & Assert
        FluentActions.Invoking(() => sut.CancelOrder(DateTime.Now))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Status Operasi tidak dapat dibatalkan*");
    }

    [Fact]
    public void ScheduleDraft_WhenRequested_ShouldAttachScheduleAndPpa_ButRetainRequestedStateWithoutHistory()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 8, 0, 0));
        var schedule = CreateSampleSchedule(orderOp); // isDraft = true
        var scheduledAt = new DateTime(2026, 10, 16, 9, 0, 0);

        schedule.IsDraft.Should().BeTrue();

        // Act
        sut.ScheduleDraft(schedule, scheduledAt);

        // Assert
        sut.OrderOpState.Should().Be(OpCaseStateEnum.Requested);
        sut.ScheduleOp.ScheduleOpId.Should().Be(schedule.ScheduleOpId);
        sut.ScheduleOp.ScheduledDate.Should().Be(schedule.TglOp);
        sut.DuranteOp.Should().Be(DuranteOpReff.Default);

        // State history should NOT add Scheduled state (still only 1 initial Requested history)
        sut.ListStateHistory.Should().HaveCount(1);
        sut.ListStateHistory.Should().NotContain(x => x.OpCaseState == OpCaseStateEnum.Scheduled);

        // Ppa team should be attached
        sut.ListPpa.Should().Contain(x => x.Ppa == schedule.TeamLead);
    }

    [Fact]
    public void ScheduleDraft_WhenNotRequested_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 8, 0, 0));
        var schedule = CreateSampleSchedule(orderOp);
        schedule.Confirm("user-conf", new DateTime(2026, 10, 16, 8, 30, 0));
        sut.ConfirmSchedule(schedule, new DateTime(2026, 10, 16, 8, 30, 0));
        sut.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);

        // Act & Assert
        FluentActions.Invoking(() => sut.ScheduleDraft(schedule, DateTime.Now))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Requested*");
    }

    [Fact]
    public void ConfirmSchedule_WhenDraftSchedule_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 8, 0, 0));
        var schedule = CreateSampleSchedule(orderOp); // isDraft == true
        schedule.IsDraft.Should().BeTrue();

        // Act & Assert
        FluentActions.Invoking(() => sut.ConfirmSchedule(schedule, DateTime.Now))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*draf*");
    }

    [Fact]
    public void ConfirmSchedule_WhenValid_ShouldTransitionToScheduledAndRecordHistory()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 8, 0, 0));
        var schedule = CreateSampleSchedule(orderOp);
        var scheduledAt = new DateTime(2026, 10, 16, 9, 30, 0);

        // First allocate as draft
        sut.ScheduleDraft(schedule, scheduledAt);
        sut.OrderOpState.Should().Be(OpCaseStateEnum.Requested);

        // Confirm the schedule model first
        schedule.Confirm("user-conf", scheduledAt);
        schedule.IsDraft.Should().BeFalse();

        // Act
        sut.ConfirmSchedule(schedule, scheduledAt);

        // Assert
        sut.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);
        sut.ScheduleOp.ScheduleOpId.Should().Be(schedule.ScheduleOpId);
        sut.ScheduleOp.ScheduledDate.Should().Be(schedule.TglOp);

        // State history should contain Scheduled state
        sut.ListStateHistory.Should().Contain(x =>
            x.OpCaseState == OpCaseStateEnum.Scheduled && x.StateTimestamp == scheduledAt);
    }

    [Fact]
    public void ConfirmSchedule_WhenNotRequested_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp();
        var sut = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 16, 8, 0, 0));
        var schedule = CreateSampleSchedule(orderOp);
        schedule.Confirm("user-conf", new DateTime(2026, 10, 16, 8, 30, 0));
        sut.ConfirmSchedule(schedule, new DateTime(2026, 10, 16, 8, 30, 0));
        sut.Start(DateTime.Now); // Now OpStarted

        var newSchedule = CreateSampleSchedule(orderOp);
        newSchedule.Confirm("user-conf", DateTime.Now);

        // Act & Assert
        FluentActions.Invoking(() => sut.ConfirmSchedule(newSchedule, DateTime.Now))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Requested*");
    }
}
