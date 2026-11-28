using Bilreg.Application.AdmisiContext.PpaFeature;
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
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkPreOpClearanceSaveHandlerTest
{
    private static readonly DateTime Now = new(2026, 11, 25, 9, 30, 0);

    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<IPreOpClearanceRepo> _clearanceRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private OkPreOpClearanceSaveHandler CreateSut()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(Now);
        return new OkPreOpClearanceSaveHandler(
            _scheduleOpRepoMock.Object,
            _orderOpRepoMock.Object,
            _opCaseRepoMock.Object,
            _clearanceRepoMock.Object,
            _ppaRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object);
    }

    private static OrderOpModel CreateSampleOrderOp(
        string orderOpId = "ORD-001",
        UrgencyLevelEnum urgency = UrgencyLevelEnum.Elective)
    {
        return new OrderOpModel(
            orderOpId,
            new DateTime(2026, 11, 25),
            AuditTrailType.Create("u1", new DateTime(2026, 11, 25, 7, 0, 0)),
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new RegReff("REG-001", "PAS-001", "Pasien Test"),
            new Icd10Type("K40", "Hernia"),
            new JenisOperasiType("JOP-1", "Bedah Umum"),
            "Herniotomi",
            urgency,
            TarifReff.Default,
            new PpaReff("DOC-01", "dr. Bedah"),
            60,
            new DateTime(2026, 11, 25),
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
        bool isDraft = false,
        AuditTrailType? auditTrail = null,
        IEnumerable<ScheduleOpPpaType>? extraPpa = null)
    {
        var listPpa = new List<ScheduleOpPpaType>
        {
            new(1, new PpaReff("DOC-01", "dr. Bedah"), ProfesiType.Dokter, GroupSpesialisType.Default)
        };
        if (extraPpa is not null)
            listPpa.AddRange(extraPpa);

        return new ScheduleOpModel(
            scheduleOpId,
            new DateTime(2026, 11, 25),
            auditTrail ?? AuditTrailType.Create("u1", new DateTime(2026, 11, 25, 7, 30, 0)),
            new OrderOpReff(orderOpId, new DateTime(2026, 11, 25), "Herniotomi"),
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            UrgencyLevelEnum.Elective,
            60,
            new DateTime(2026, 11, 25, 10, 0, 0),
            new KamarReff("KAMAR-1", "OK 1"),
            new RegReff("REG-001", "PAS-001", "Pasien Test"),
            new PpaReff("DOC-01", "dr. Bedah"),
            new DateTime(2026, 11, 25, 10, 0, 0),
            new DateTime(2026, 11, 25, 11, 0, 0),
            OpCaseStateEnum.Scheduled,
            listPpa,
            isDraft: isDraft);
    }

    private static OpCaseModel CreateScheduledOpCase(OrderOpModel orderOp, ScheduleOpModel scheduleOp)
    {
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 11, 25, 7, 0, 0));
        opCase.Schedule(scheduleOp, new DateTime(2026, 11, 25, 8, 0, 0));
        return opCase;
    }

    private static PpaType CreatePpa(string ppaId, ProfesiType profesi)
    {
        var satTugas = new SatTugasType("ST-1", profesi.ProfesiName, profesi);
        return new PpaType(
            ppaId,
            $"PPA {ppaId}",
            ppaId,
            SmfType.Default,
            GroupSpesialisType.Default,
            [],
            [new PpaSatTugasType(satTugas, true)],
            []);
    }

    private static List<ChecklistItemInput> CompleteChecklist() =>
    [
        new(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Verified, "Cocok"),
        new(PreOpChecklistItemType.InformedConsent, ChecklistItemStatusEnum.Verified, "Lengkap"),
        new(PreOpChecklistItemType.AnesthesiaClearance, ChecklistItemStatusEnum.Cleared, "Acc Anestesi"),
        new(PreOpChecklistItemType.PhysicalPrep, ChecklistItemStatusEnum.Verified, "Puasa 8 jam"),
        new(PreOpChecklistItemType.BloodLogistics, ChecklistItemStatusEnum.Verified, "Siap")
    ];

    private void SetupStandardGraph(OrderOpModel orderOp, OpCaseModel opCase, ScheduleOpModel scheduleOp)
    {
        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == orderOp.OrderOpId)))
            .Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(orderOp))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == scheduleOp.ScheduleOpId)))
            .Returns(MayBe.From(scheduleOp));
    }

    [Fact]
    public async Task Handle_PendingClearance_FirstAttempt_CreatesNoUrut1AndDoesNotAdvanceOpCaseOrPublish()
    {
        var orderOp = CreateSampleOrderOp();
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);

        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);
        _clearanceRepoMock.Setup(x => x.CountByScheduleOpId("SCH-001"))
            .Returns(0);

        var cmd = new OkPreOpClearanceSaveCommand(
            OrderOpId: "ORD-001",
            UserId: "perawat-01",
            ChecklistItems:
            [
                new(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Verified, "OK"),
                new(PreOpChecklistItemType.InformedConsent, ChecklistItemStatusEnum.Pending, "Menunggu tanda tangan")
            ],
            VerifikatorPpaId: "PRW-01",
            DokterPjId: "DOC-01",
            FinalStatus: null);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.NoUrut.Should().Be(1);
        result.ClearanceStatus.Should().Be("PENDING_CLEARANCE");
        result.OpCaseState.Should().Be("Scheduled");
        result.ChecklistItems.Should().HaveCount(2);
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);

        _clearanceRepoMock.Verify(x => x.SaveChanges(It.Is<PreOpClearanceModel>(
            m => m.NoUrut == 1 && m.ClearanceStatus == PreOpClearanceStatusEnum.PendingClearance)), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<OkPreOpClearedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PendingClearance_WhenExistingPending_UpdatesSameRecordWithoutIncrementingNoUrut()
    {
        var orderOp = CreateSampleOrderOp();
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);

        var existingPending = PreOpClearanceModel.Create(
            "SCH-001", "ORD-001", "REG-001", 1, "PRW-01", "DOC-01", "perawat-01", Now.AddMinutes(-20));
        existingPending.UpdateChecklist(
            [new(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Pending, "")],
            "perawat-01", Now.AddMinutes(-20));

        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe.From(existingPending));

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001",
            "perawat-02",
            [new(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Verified, "Sudah dicek")],
            "PRW-01",
            "DOC-01",
            "PENDING_CLEARANCE");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.ClearanceId.Should().Be(existingPending.ClearanceId);
        result.NoUrut.Should().Be(1);
        result.ClearanceStatus.Should().Be("PENDING_CLEARANCE");
        _clearanceRepoMock.Verify(x => x.CountByScheduleOpId(It.IsAny<string>()), Times.Never);
        _clearanceRepoMock.Verify(x => x.SaveChanges(existingPending), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<OkPreOpClearedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AfterNotCleared_CreatesNewRecordWithIncrementedNoUrut()
    {
        var orderOp = CreateSampleOrderOp();
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);

        var previousNotCleared = PreOpClearanceModel.Create(
            "SCH-001", "ORD-001", "REG-001", 1, "PRW-01", "DOC-01", "perawat-01", Now.AddHours(-1));
        previousNotCleared.Reject("Puasa belum cukup", "perawat-01", Now.AddMinutes(-50));

        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe.From(previousNotCleared));
        _clearanceRepoMock.Setup(x => x.CountByScheduleOpId("SCH-001"))
            .Returns(1);

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001",
            "perawat-02",
            CompleteChecklist(),
            "PRW-02",
            "DOC-01",
            "CLEARED");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.ClearanceId.Should().NotBe(previousNotCleared.ClearanceId);
        result.NoUrut.Should().Be(2);
        result.ClearanceStatus.Should().Be("CLEARED");
        result.OpCaseState.Should().Be("PreOpCleared");
    }

    [Fact]
    public async Task Handle_Cleared_AdvancesOpCaseToPreOpClearedAndPublishesEventOnceWithoutModifyingOrderOrSchedule()
    {
        var orderOp = CreateSampleOrderOp();
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);

        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);
        _clearanceRepoMock.Setup(x => x.CountByScheduleOpId("SCH-001"))
            .Returns(0);

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001",
            "perawat-01",
            CompleteChecklist(),
            "PRW-01",
            "DOC-01",
            "CLEARED");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.ClearanceStatus.Should().Be("CLEARED");
        result.OpCaseState.Should().Be("PreOpCleared");
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.PreOpCleared);

        _clearanceRepoMock.Verify(x => x.SaveChanges(It.Is<PreOpClearanceModel>(
            m => m.ClearanceStatus == PreOpClearanceStatusEnum.Cleared)), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.Is<OpCaseModel>(
            m => m.OrderOpState == OpCaseStateEnum.PreOpCleared)), Times.Once);
        _orderOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderOpModel>()), Times.Never);
        _scheduleOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.Never);
        _mediatorMock.Verify(x => x.Publish(
            It.Is<OkPreOpClearedEvent>(ev =>
                ev.Clearance.ClearanceId == result.ClearanceId &&
                ev.OccurredAt == Now &&
                ev.Pasien == orderOp.Pasien &&
                ev.TglOp == scheduleOp.TglOp),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ClearedWithIncompleteChecklist_ThrowsPreOpChecklistIncompleteExceptionWithoutSaving()
    {
        var orderOp = CreateSampleOrderOp();
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);

        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);
        _clearanceRepoMock.Setup(x => x.CountByScheduleOpId("SCH-001"))
            .Returns(0);

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001",
            "perawat-01",
            [new(PreOpChecklistItemType.InformedConsent, ChecklistItemStatusEnum.Failed, "Belum ditandatangani")],
            "PRW-01",
            "DOC-01",
            "CLEARED");

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<PreOpChecklistIncompleteException>();
        _clearanceRepoMock.Verify(x => x.SaveChanges(It.IsAny<PreOpClearanceModel>()), Times.Never);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<OkPreOpClearedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotCleared_SavesClearanceOnlyAndDoesNotModifyOpCaseOrPublishEvent()
    {
        var orderOp = CreateSampleOrderOp();
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);

        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);
        _clearanceRepoMock.Setup(x => x.CountByScheduleOpId("SCH-001"))
            .Returns(0);

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001",
            "perawat-01",
            [new(PreOpChecklistItemType.AnesthesiaClearance, ChecklistItemStatusEnum.Failed, "Tekanan darah tinggi")],
            "PRW-01",
            "DOC-01",
            FinalStatus: "NOT_CLEARED",
            AlasanNotCleared: "Hemodinamik tidak stabil");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.ClearanceStatus.Should().Be("NOT_CLEARED");
        result.AlasanNotCleared.Should().Be("Hemodinamik tidak stabil");
        result.OpCaseState.Should().Be("Scheduled");
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);

        _clearanceRepoMock.Verify(x => x.SaveChanges(It.Is<PreOpClearanceModel>(
            m => m.ClearanceStatus == PreOpClearanceStatusEnum.NotCleared)), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _orderOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderOpModel>()), Times.Never);
        _scheduleOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.Never);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<OkPreOpClearedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("DOC-01")] // TeamLead
    [InlineData("DOC-ANEST")] // Dokter anggota ListPpa
    public async Task Handle_EmergencyOverridden_SucceedsForCitoAndAuthorizedScheduleDoctor(string dokterOverrideId)
    {
        var orderOp = CreateSampleOrderOp(urgency: UrgencyLevelEnum.Cito);
        var scheduleOp = CreateSampleScheduleOp(extraPpa:
        [
            new(2, new PpaReff("DOC-ANEST", "dr. Anestesi"), ProfesiType.Dokter, GroupSpesialisType.Default),
            new(3, new PpaReff("PRW-01", "Ns. Bedah"), ProfesiType.Perawat, GroupSpesialisType.Default)
        ]);
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);

        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);
        _clearanceRepoMock.Setup(x => x.CountByScheduleOpId("SCH-001"))
            .Returns(0);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == dokterOverrideId)))
            .Returns(MayBe.From(CreatePpa(dokterOverrideId, ProfesiType.Dokter)));

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001",
            "perawat-01",
            [new(PreOpChecklistItemType.BloodLogistics, ChecklistItemStatusEnum.Pending, "")],
            "PRW-01",
            "DOC-01",
            FinalStatus: "EMERGENCY_OVERRIDDEN",
            DokterOverrideId: dokterOverrideId,
            AlasanOverride: "Perdarahan aktif mengancam nyawa pasien");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.ClearanceStatus.Should().Be("EMERGENCY_OVERRIDDEN");
        result.DokterOverrideId.Should().Be(dokterOverrideId);
        result.OpCaseState.Should().Be("PreOpCleared");
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.PreOpCleared);

        _clearanceRepoMock.Verify(x => x.SaveChanges(It.IsAny<PreOpClearanceModel>()), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(opCase), Times.Once);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<OkPreOpClearedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmergencyOverridden_WhenDokterOverrideIdEmpty_ThrowsUnauthorizedAccessException()
    {
        var orderOp = CreateSampleOrderOp(urgency: UrgencyLevelEnum.Cito);
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);
        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001", "perawat-01", CompleteChecklist(), "PRW-01", "DOC-01",
            FinalStatus: "EMERGENCY_OVERRIDDEN",
            DokterOverrideId: "  ",
            AlasanOverride: "Kondisi gawat darurat akut");

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _clearanceRepoMock.Verify(x => x.SaveChanges(It.IsAny<PreOpClearanceModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmergencyOverridden_WhenPpaNotFound_ThrowsUnauthorizedAccessException()
    {
        var orderOp = CreateSampleOrderOp(urgency: UrgencyLevelEnum.Cito);
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);
        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe<PpaType>.None);

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001", "perawat-01", CompleteChecklist(), "PRW-01", "DOC-01",
            FinalStatus: "EMERGENCY_OVERRIDDEN",
            DokterOverrideId: "UNKNOWN-PPA",
            AlasanOverride: "Kondisi gawat darurat akut");

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_EmergencyOverridden_WhenPpaIsNotDokter_ThrowsUnauthorizedAccessException()
    {
        var orderOp = CreateSampleOrderOp(urgency: UrgencyLevelEnum.Cito);
        var scheduleOp = CreateSampleScheduleOp(extraPpa:
        [
            new(2, new PpaReff("PRW-01", "Ns. Bedah"), ProfesiType.Perawat, GroupSpesialisType.Default)
        ]);
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);
        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "PRW-01")))
            .Returns(MayBe.From(CreatePpa("PRW-01", ProfesiType.Perawat)));

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001", "perawat-01", CompleteChecklist(), "PRW-01", "DOC-01",
            FinalStatus: "EMERGENCY_OVERRIDDEN",
            DokterOverrideId: "PRW-01",
            AlasanOverride: "Kondisi gawat darurat akut");

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_EmergencyOverridden_WhenDoctorNotInScheduleTeam_ThrowsUnauthorizedAccessException()
    {
        var orderOp = CreateSampleOrderOp(urgency: UrgencyLevelEnum.Cito);
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);
        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-OUTSIDER")))
            .Returns(MayBe.From(CreatePpa("DOC-OUTSIDER", ProfesiType.Dokter)));

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001", "perawat-01", CompleteChecklist(), "PRW-01", "DOC-01",
            FinalStatus: "EMERGENCY_OVERRIDDEN",
            DokterOverrideId: "DOC-OUTSIDER",
            AlasanOverride: "Kondisi gawat darurat akut");

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_EmergencyOverridden_WhenElectiveCase_ThrowsInvalidOperationException()
    {
        var orderOp = CreateSampleOrderOp(urgency: UrgencyLevelEnum.Elective);
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);
        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(CreatePpa("DOC-01", ProfesiType.Dokter)));

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001", "perawat-01", CompleteChecklist(), "PRW-01", "DOC-01",
            FinalStatus: "EMERGENCY_OVERRIDDEN",
            DokterOverrideId: "DOC-01",
            AlasanOverride: "Kondisi gawat darurat akut");

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cito*");
    }

    [Fact]
    public async Task Handle_WhenScheduleIsDraft_ThrowsInvalidOperationException()
    {
        var orderOp = CreateSampleOrderOp();
        var draftSchedule = CreateSampleScheduleOp(isDraft: true);
        var opCase = CreateScheduledOpCase(orderOp, draftSchedule);
        SetupStandardGraph(orderOp, opCase, draftSchedule);

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001", "perawat-01", CompleteChecklist(), "PRW-01", "DOC-01", "CLEARED");

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DRAFT*");
        _clearanceRepoMock.Verify(x => x.SaveChanges(It.IsAny<PreOpClearanceModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenScheduleVoidedViaSqlHydratedAuditTrailVoided_ThrowsInvalidOperationException()
    {
        var orderOp = CreateSampleOrderOp();
        // 3-arg AuditTrailType leaves IsVoided = false while Voided is populated (SQL hydration pattern)
        var sqlVoidedAudit = new AuditTrailType(
            new AuditInfoType("u1", Now.AddHours(-2)),
            AuditInfoType.Default,
            new AuditInfoType("u2", Now.AddHours(-1)));
        var voidedSchedule = CreateSampleScheduleOp(auditTrail: sqlVoidedAudit);
        var opCase = CreateScheduledOpCase(orderOp, voidedSchedule);
        SetupStandardGraph(orderOp, opCase, voidedSchedule);

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001", "perawat-01", CompleteChecklist(), "PRW-01", "DOC-01", "CLEARED");

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*void*");
        _clearanceRepoMock.Verify(x => x.SaveChanges(It.IsAny<PreOpClearanceModel>()), Times.Never);
    }

    [Theory]
    [InlineData(OpCaseStateEnum.Requested)]
    [InlineData(OpCaseStateEnum.PreOpCleared)]
    [InlineData(OpCaseStateEnum.OpStarted)]
    public async Task Handle_WhenOpCaseStateInvalidForClearance_ThrowsInvalidOperationException(OpCaseStateEnum targetState)
    {
        var orderOp = CreateSampleOrderOp();
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = OpCaseModel.Create(orderOp, Now.AddHours(-3));

        if (targetState >= OpCaseStateEnum.Scheduled)
            opCase.Schedule(scheduleOp, Now.AddHours(-2));
        if (targetState >= OpCaseStateEnum.PreOpCleared)
            opCase.PreOpClear("u1", Now.AddHours(-1));
        if (targetState >= OpCaseStateEnum.OpStarted)
            opCase.Start(Now.AddMinutes(-30));

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(orderOp))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == "PAS-001")))
            .Returns(
            [
                new ScheduleOpView(
                    "SCH-001", orderOp.Pasien, orderOp.ToReff(), orderOp.UrgencyLevel,
                    scheduleOp.TglOp, scheduleOp.Durasi, scheduleOp.TeamLead, scheduleOp.KamarOp,
                    targetState, false, IsDraft: false)
            ]);

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001", "perawat-01", CompleteChecklist(), "PRW-01", "DOC-01", "CLEARED");

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _clearanceRepoMock.Verify(x => x.SaveChanges(It.IsAny<PreOpClearanceModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFinalStatusUnknown_ThrowsArgumentException()
    {
        var orderOp = CreateSampleOrderOp();
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = CreateScheduledOpCase(orderOp, scheduleOp);
        SetupStandardGraph(orderOp, opCase, scheduleOp);
        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe<PreOpClearanceModel>.None);

        var cmd = new OkPreOpClearanceSaveCommand(
            "ORD-001", "perawat-01", CompleteChecklist(), "PRW-01", "DOC-01", "UNKNOWN_STATUS");

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _clearanceRepoMock.Verify(x => x.SaveChanges(It.IsAny<PreOpClearanceModel>()), Times.Never);
    }
}
