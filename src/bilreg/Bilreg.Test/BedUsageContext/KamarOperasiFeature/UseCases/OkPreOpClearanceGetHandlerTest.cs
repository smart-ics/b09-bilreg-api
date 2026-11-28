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

public class OkPreOpClearanceGetHandlerTest
{
    private readonly Mock<IPreOpClearanceRepo> _clearanceRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();

    private OkPreOpClearanceGetHandler CreateSut() =>
        new(_clearanceRepoMock.Object, _opCaseRepoMock.Object, _scheduleOpRepoMock.Object);

    private static PreOpClearanceModel CreateSampleClearance()
    {
        var model = PreOpClearanceModel.Create(
            "SCH-001", "ORD-001", "REG-001", 1,
            "PRW-01", "DOC-01", "perawat-01", new DateTime(2026, 11, 25, 8, 0, 0));
        model.UpdateChecklist(
            [
                new(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Verified, "OK"),
                new(PreOpChecklistItemType.InformedConsent, ChecklistItemStatusEnum.Verified, "Tanda tangan lengkap")
            ],
            "perawat-01",
            new DateTime(2026, 11, 25, 8, 15, 0));
        model.Approve("perawat-01", new DateTime(2026, 11, 25, 8, 30, 0));
        return model;
    }

    private static OrderOpModel CreateSampleOrderOp() =>
        new(
            "ORD-001",
            new DateTime(2026, 11, 25),
            AuditTrailType.Create("u1", new DateTime(2026, 11, 25)),
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new RegReff("REG-001", "PAS-001", "Pasien Test"),
            new Icd10Type("K40", "Hernia"),
            new JenisOperasiType("JOP-1", "Bedah Umum"),
            "Herniotomi",
            UrgencyLevelEnum.Elective,
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

    private static ScheduleOpModel CreateSampleScheduleOp() =>
        new(
            "SCH-001",
            new DateTime(2026, 11, 25),
            AuditTrailType.Create("u1", new DateTime(2026, 11, 25)),
            new OrderOpReff("ORD-001", new DateTime(2026, 11, 25), "Herniotomi"),
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
            [new(1, new PpaReff("DOC-01", "dr. Bedah"), ProfesiType.Dokter, GroupSpesialisType.Default)],
            isDraft: false);

    [Fact]
    public async Task Handle_WithScheduleOpId_ReturnsLatestClearanceAndChecklist()
    {
        var clearance = CreateSampleClearance();
        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe.From(clearance));

        var result = await CreateSut().Handle(
            new OkPreOpClearanceGetQuery(ScheduleOpId: "SCH-001"),
            CancellationToken.None);

        result.ClearanceId.Should().Be(clearance.ClearanceId);
        result.ScheduleOpId.Should().Be("SCH-001");
        result.OrderOpId.Should().Be("ORD-001");
        result.ClearanceStatus.Should().Be("CLEARED");
        result.ChecklistItems.Should().HaveCount(2);
        _opCaseRepoMock.Verify(x => x.LoadEntity(It.IsAny<IOrderOpKey>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithOrderOpId_ResolvesScheduleOpIdFromOpCaseAndReturnsClearance()
    {
        var orderOp = CreateSampleOrderOp();
        var scheduleOp = CreateSampleScheduleOp();
        var opCase = OpCaseModel.Create(orderOp);
        opCase.Schedule(scheduleOp, new DateTime(2026, 11, 25, 7, 30, 0));

        var clearance = CreateSampleClearance();
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));
        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe.From(clearance));

        var result = await CreateSut().Handle(
            new OkPreOpClearanceGetQuery(OrderOpId: "ORD-001"),
            CancellationToken.None);

        result.ClearanceId.Should().Be(clearance.ClearanceId);
        result.ScheduleOpId.Should().Be("SCH-001");
        result.ChecklistItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithOrderOpIdWhenOpCaseScheduleDefault_FallsBackToScheduleRepoListData()
    {
        var orderOp = CreateSampleOrderOp();
        var opCase = OpCaseModel.Create(orderOp); // ScheduleOp.ScheduleOpId == "-"
        var clearance = CreateSampleClearance();

        _opCaseRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == "PAS-001")))
            .Returns(
            [
                new ScheduleOpView(
                    "SCH-001", orderOp.Pasien, orderOp.ToReff(), orderOp.UrgencyLevel,
                    new DateTime(2026, 11, 25, 10, 0, 0), 60,
                    new PpaReff("DOC-01", "dr. Bedah"), new KamarReff("KAMAR-1", "OK 1"),
                    OpCaseStateEnum.Scheduled, false, IsDraft: false)
            ]);
        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-001"))
            .Returns(MayBe.From(clearance));

        var result = await CreateSut().Handle(
            new OkPreOpClearanceGetQuery(OrderOpId: "ORD-001"),
            CancellationToken.None);

        result.ClearanceId.Should().Be(clearance.ClearanceId);
        result.ScheduleOpId.Should().Be("SCH-001");
    }

    [Fact]
    public async Task Handle_WhenClearanceNotFound_ThrowsKeyNotFoundException()
    {
        _clearanceRepoMock.Setup(x => x.GetLatestByScheduleOpId("SCH-404"))
            .Returns(MayBe<PreOpClearanceModel>.None);

        var act = async () => await CreateSut().Handle(
            new OkPreOpClearanceGetQuery(ScheduleOpId: "SCH-404"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenOrderOpHasNoSchedule_ThrowsKeyNotFoundException()
    {
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-404")))
            .Returns(MayBe<OpCaseModel>.None);

        var act = async () => await CreateSut().Handle(
            new OkPreOpClearanceGetQuery(OrderOpId: "ORD-404"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenBothIdsMissing_ThrowsArgumentException()
    {
        var act = async () => await CreateSut().Handle(
            new OkPreOpClearanceGetQuery(),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
