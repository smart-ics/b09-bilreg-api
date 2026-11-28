using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardFeature;
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

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkStartOpHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<IPreOpClearanceRepo> _preOpClearanceRepoMock = new();
    private readonly Mock<IStartOpRepo> _startOpRepoMock = new();
    private readonly Mock<IKamarRepo> _kamarRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private OkStartOpHandler CreateSut()
    {
        return new OkStartOpHandler(
            _scheduleOpRepoMock.Object,
            _orderOpRepoMock.Object,
            _opCaseRepoMock.Object,
            _preOpClearanceRepoMock.Object,
            _startOpRepoMock.Object,
            _kamarRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    private static OrderOpModel CreateSampleOrderOp(string orderOpId = "ORD-001")
    {
        return new OrderOpModel(
            orderOpId,
            new DateTime(2026, 10, 20),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 20)),
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new RegReff("REG-001", "PAS-001", "Pasien Test"),
            new Icd10Type("K40", "Hernia"),
            new JenisOperasiType("JOP-1", "Bedah Umum"),
            "Herniotomi",
            UrgencyLevelEnum.Elective,
            TarifReff.Default,
            new PpaReff("DOC-01", "dr. Bedah"),
            60,
            new DateTime(2026, 10, 20),
            "-",
            new PpaReff("DOC-01", "dr. Bedah"),
            new LayananReff("LYN-01", "Klinik Bedah"),
            "",
            KlasifikasiOperasiEnum.Sedang,
            AnestesiPlanEnum.General,
            PostOpCareUnitEnum.PACU,
            "",
            []);
    }

    private static ScheduleOpModel CreateSampleScheduleOp(
        string scheduleOpId = "SCH-001",
        string orderOpId = "ORD-001",
        bool isDraft = false)
    {
        return new ScheduleOpModel(
            scheduleOpId,
            new DateTime(2026, 10, 20),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 20)),
            new OrderOpReff(orderOpId, new DateTime(2026, 10, 20), "Herniotomi"),
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            UrgencyLevelEnum.Elective,
            60,
            new DateTime(2026, 10, 20),
            new KamarReff("KAMAR-1", "OK 1"),
            new RegReff("REG-001", "PAS-001", "Pasien Test"),
            new PpaReff("DOC-01", "dr. Bedah"),
            new DateTime(2026, 10, 20, 8, 0, 0),
            new DateTime(2026, 10, 20, 9, 0, 0),
            OpCaseStateEnum.Scheduled,
            [],
            isDraft: isDraft);
    }

    private static PreOpClearanceModel CreateSampleClearance(
        PreOpClearanceStatusEnum status,
        string clearanceId = "CLR-001",
        string scheduleOpId = "SCH-001",
        string orderOpId = "ORD-001")
    {
        return new PreOpClearanceModel(
            clearanceId,
            scheduleOpId,
            orderOpId,
            "REG-001",
            1,
            status,
            [new PreOpChecklistItemType(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Verified, "OK")],
            "PPA-01",
            "DOC-01",
            status == PreOpClearanceStatusEnum.PendingClearance
                ? new DateTime(3000, 1, 1)
                : new DateTime(2026, 10, 20, 7, 45, 0),
            status == PreOpClearanceStatusEnum.EmergencyOverridden ? "DOC-01" : "",
            status == PreOpClearanceStatusEnum.EmergencyOverridden ? "Kondisi Cito mengancam nyawa" : "",
            status == PreOpClearanceStatusEnum.NotCleared ? "Puasa belum terpenuhi" : "",
            AuditTrailType.Create("u1", new DateTime(2026, 10, 20, 7, 30, 0)));
    }

    private void SetupBaseMocks(OrderOpModel orderOp, OpCaseModel opCase, ScheduleOpModel scheduleOp)
    {
        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == orderOp.OrderOpId)))
            .Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(orderOp))
            .Returns(MayBe.From(opCase));

        var scheduleView = new ScheduleOpView(
            scheduleOp.ScheduleOpId,
            orderOp.Pasien,
            scheduleOp.OrderOp,
            scheduleOp.UrgencyLevel,
            scheduleOp.TglOp,
            scheduleOp.Durasi,
            scheduleOp.TeamLead,
            scheduleOp.KamarOp,
            opCase.OrderOpState,
            false,
            IsDraft: scheduleOp.IsDraft);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == orderOp.Pasien.PasienId)))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == scheduleOp.ScheduleOpId)))
            .Returns(MayBe.From(scheduleOp));

        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == scheduleOp.KamarOp.KamarId)))
            .Returns(MayBe.From(new KamarType(
                scheduleOp.KamarOp.KamarId,
                scheduleOp.KamarOp.KamarName,
                new BangsalReff("BSL-OK", "Instalasi Bedah Sentral"),
                new KelasReff("KLS-OK", "Kamar Operasi"))));

        _tglJamProviderMock.Setup(x => x.Now)
            .Returns(new DateTime(2026, 10, 20, 8, 15, 0));
    }

    [Fact]
    public async Task Handle_WhenScheduleIsDraft_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var orderOp = CreateSampleOrderOp();
        var opCase = OpCaseModel.Create(orderOp);
        var draftSchedule = CreateSampleScheduleOp(isDraft: true);
        SetupBaseMocks(orderOp, opCase, draftSchedule);

        var cmd = new OkStartOpCommand("ORD-001", "08:15", "user-01");
        var sut = CreateSut();

        // ACT
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DRAFT*");
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _startOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<StartOpModel>()), Times.Never);
    }

    [Theory]
    [InlineData(OpCaseStateEnum.Requested)]
    [InlineData(OpCaseStateEnum.Scheduled)]
    [InlineData(OpCaseStateEnum.OpStarted)]
    [InlineData(OpCaseStateEnum.Cancelled)]
    public async Task Handle_WhenOpCaseStateNotPreOpCleared_ThrowsInvalidOperationException(OpCaseStateEnum state)
    {
        // ARRANGE
        var orderOp = CreateSampleOrderOp();
        var schedule = CreateSampleScheduleOp(isDraft: false);
        var opCase = OpCaseModel.Create(orderOp);
        switch (state)
        {
            case OpCaseStateEnum.Scheduled:
                opCase.Schedule(schedule, new DateTime(2026, 10, 20, 7, 0, 0));
                break;
            case OpCaseStateEnum.OpStarted:
                opCase.Schedule(schedule, new DateTime(2026, 10, 20, 7, 0, 0));
                opCase.PreOpClear("u1", new DateTime(2026, 10, 20, 7, 30, 0));
                opCase.Start(new DateTime(2026, 10, 20, 8, 0, 0));
                break;
            case OpCaseStateEnum.Cancelled:
                opCase.CancelOrder(new DateTime(2026, 10, 20, 7, 0, 0));
                break;
        }

        SetupBaseMocks(orderOp, opCase, schedule);
        _preOpClearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe.From(CreateSampleClearance(PreOpClearanceStatusEnum.Cleared)));

        var cmd = new OkStartOpCommand("ORD-001", "08:15", "user-01");
        var sut = CreateSut();

        // ACT
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PreOpCleared*");
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _startOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<StartOpModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenScheduleCancelledAfterPreOpCleared_ThrowsInvalidOperationException()
    {
        // ARRANGE (AC-07: CancelSchedule reverts OpCase to Requested even though clearance archive stays non-void)
        var orderOp = CreateSampleOrderOp();
        var schedule = CreateSampleScheduleOp(isDraft: false);
        var opCase = OpCaseModel.Create(orderOp);
        opCase.Schedule(schedule, new DateTime(2026, 10, 20, 7, 0, 0));
        opCase.PreOpClear("u1", new DateTime(2026, 10, 20, 7, 30, 0));
        opCase.CancelSchedule();

        SetupBaseMocks(orderOp, opCase, schedule);
        _preOpClearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe.From(CreateSampleClearance(PreOpClearanceStatusEnum.Cleared)));

        var cmd = new OkStartOpCommand("ORD-001", "08:15", "user-01");
        var sut = CreateSut();

        // ACT
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PreOpCleared*");
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _startOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<StartOpModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenClearanceNotFound_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var orderOp = CreateSampleOrderOp();
        var schedule = CreateSampleScheduleOp(isDraft: false);
        var opCase = OpCaseModel.Create(orderOp);
        opCase.Schedule(schedule, new DateTime(2026, 10, 20, 7, 0, 0));
        opCase.PreOpClear("u1", new DateTime(2026, 10, 20, 7, 30, 0));

        SetupBaseMocks(orderOp, opCase, schedule);
        _preOpClearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);

        var cmd = new OkStartOpCommand("ORD-001", "08:15", "user-01");
        var sut = CreateSut();

        // ACT
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pre-Operative Clearance*");
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _startOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<StartOpModel>()), Times.Never);
    }

    [Theory]
    [InlineData(PreOpClearanceStatusEnum.PendingClearance)]
    [InlineData(PreOpClearanceStatusEnum.NotCleared)]
    public async Task Handle_WhenClearanceStatusNotClearedOrOverridden_ThrowsInvalidOperationException(
        PreOpClearanceStatusEnum clearanceStatus)
    {
        // ARRANGE
        var orderOp = CreateSampleOrderOp();
        var schedule = CreateSampleScheduleOp(isDraft: false);
        var opCase = OpCaseModel.Create(orderOp);
        opCase.Schedule(schedule, new DateTime(2026, 10, 20, 7, 0, 0));
        opCase.PreOpClear("u1", new DateTime(2026, 10, 20, 7, 30, 0));

        SetupBaseMocks(orderOp, opCase, schedule);
        _preOpClearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe.From(CreateSampleClearance(clearanceStatus)));

        var cmd = new OkStartOpCommand("ORD-001", "08:15", "user-01");
        var sut = CreateSut();

        // ACT
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CLEARED atau EMERGENCY_OVERRIDDEN*");
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _startOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<StartOpModel>()), Times.Never);
    }

    [Theory]
    [InlineData(PreOpClearanceStatusEnum.Cleared)]
    [InlineData(PreOpClearanceStatusEnum.EmergencyOverridden)]
    public async Task Handle_WhenPreOpClearedAndActiveClearanceValid_StartsOperationAndSavesStartOpWithClearanceId(
        PreOpClearanceStatusEnum clearanceStatus)
    {
        // ARRANGE
        var orderOp = CreateSampleOrderOp();
        var schedule = CreateSampleScheduleOp(isDraft: false);
        var opCase = OpCaseModel.Create(orderOp);
        opCase.Schedule(schedule, new DateTime(2026, 10, 20, 7, 0, 0));
        opCase.PreOpClear("u1", new DateTime(2026, 10, 20, 7, 30, 0));

        var activeClearance = CreateSampleClearance(clearanceStatus, clearanceId: "CLR-ACTIVE-01");
        SetupBaseMocks(orderOp, opCase, schedule);
        _preOpClearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe.From(activeClearance));

        var expectedNow = new DateTime(2026, 10, 20, 8, 15, 0);
        var cmd = new OkStartOpCommand("ORD-001", "08:15", "user-01");
        var sut = CreateSut();

        // ACT
        var result = await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        result.Should().NotBeNull();
        result.OrderOpId.Should().Be("ORD-001");
        result.ScheduleOpId.Should().Be("SCH-001");
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.OpStarted);
        _opCaseRepoMock.Verify(
            x => x.SaveChanges(It.Is<OpCaseModel>(m => m.OrderOpState == OpCaseStateEnum.OpStarted)),
            Times.Once);
        _startOpRepoMock.Verify(
            x => x.SaveChanges(It.Is<StartOpModel>(s =>
                s.ClearanceId == activeClearance.ClearanceId &&
                s.ScheduleOp.ScheduleOpId == "SCH-001" &&
                s.OrderOp.OrderOpId == "ORD-001" &&
                s.KamarOp.KamarId == "KAMAR-1" &&
                s.AuditTrail.Created.UserId == "user-01" &&
                s.AuditTrail.Created.Timestamp == expectedNow)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenKamarNotFound_FallbacksToDefaultKamarAndStillSavesStartOp()
    {
        // ARRANGE
        var orderOp = CreateSampleOrderOp();
        var schedule = CreateSampleScheduleOp(isDraft: false);
        var opCase = OpCaseModel.Create(orderOp);
        opCase.Schedule(schedule, new DateTime(2026, 10, 20, 7, 0, 0));
        opCase.PreOpClear("u1", new DateTime(2026, 10, 20, 7, 30, 0));

        var activeClearance = CreateSampleClearance(PreOpClearanceStatusEnum.Cleared, clearanceId: "CLR-002");
        SetupBaseMocks(orderOp, opCase, schedule);
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>()))
            .Returns(MayBe<KamarType>.None);
        _preOpClearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe.From(activeClearance));

        var cmd = new OkStartOpCommand("ORD-001", "08:15", "user-01");
        var sut = CreateSut();

        // ACT
        var result = await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        result.OrderOpId.Should().Be("ORD-001");
        _startOpRepoMock.Verify(
            x => x.SaveChanges(It.Is<StartOpModel>(s =>
                s.ClearanceId == "CLR-002" &&
                s.KamarOp.KamarId == KamarType.Default.KamarId)),
            Times.Once);
    }
}
