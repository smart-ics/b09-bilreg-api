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

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkStartOpHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();

    private OkStartOpHandler CreateSut()
    {
        return new OkStartOpHandler(
            _scheduleOpRepoMock.Object,
            _orderOpRepoMock.Object,
            _opCaseRepoMock.Object);
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

    [Fact]
    public async Task Handle_WhenScheduleIsDraft_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp); // OrderOpState = Requested

        var draftSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", isDraft: true);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(orderOp))
            .Returns(MayBe.From(opCase));

        var scheduleView = new ScheduleOpView(
            "SCH-001",
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new OrderOpReff("ORD-001", new DateTime(2026, 10, 20), "Herniotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 0, 0),
            60,
            new PpaReff("DOC-01", "dr. Bedah"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Requested,
            false,
            IsDraft: true);
        _scheduleOpRepoMock.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == "PAS-001")))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(draftSchedule));

        var cmd = new OkStartOpCommand("ORD-001", "08:15", "user-01");
        var sut = CreateSut();

        // ACT
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DRAFT*");
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOpCaseStateNotScheduled_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp); // OrderOpState = Requested
        var nonDraftSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", isDraft: false);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(orderOp))
            .Returns(MayBe.From(opCase));

        var scheduleView = new ScheduleOpView(
            "SCH-001",
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new OrderOpReff("ORD-001", new DateTime(2026, 10, 20), "Herniotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 0, 0),
            60,
            new PpaReff("DOC-01", "dr. Bedah"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Requested,
            false,
            IsDraft: false);
        _scheduleOpRepoMock.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == "PAS-001")))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(nonDraftSchedule));

        var cmd = new OkStartOpCommand("ORD-001", "08:15", "user-01");
        var sut = CreateSut();

        // ACT
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DRAFT*");
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenScheduleConfirmedAndScheduled_SuccessfullyStartsOperation()
    {
        // ARRANGE
        var orderOp = CreateSampleOrderOp("ORD-001");
        var confirmedSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", isDraft: false);
        var opCase = OpCaseModel.Create(orderOp);
        opCase.Schedule(confirmedSchedule, new DateTime(2026, 10, 20, 8, 0, 0)); // Sets OrderOpState = Scheduled

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(orderOp))
            .Returns(MayBe.From(opCase));

        var scheduleView = new ScheduleOpView(
            "SCH-001",
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new OrderOpReff("ORD-001", new DateTime(2026, 10, 20), "Herniotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 0, 0),
            60,
            new PpaReff("DOC-01", "dr. Bedah"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            false,
            IsDraft: false);
        _scheduleOpRepoMock.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == "PAS-001")))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(confirmedSchedule));

        var cmd = new OkStartOpCommand("ORD-001", "08:15", "user-01");
        var sut = CreateSut();

        // ACT
        var result = await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        result.Should().NotBeNull();
        result.OrderOpId.Should().Be("ORD-001");
        result.ScheduleOpId.Should().Be("SCH-001");
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.OpStarted);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.Is<OpCaseModel>(m => m.OrderOpState == OpCaseStateEnum.OpStarted)), Times.Once);
    }
}
