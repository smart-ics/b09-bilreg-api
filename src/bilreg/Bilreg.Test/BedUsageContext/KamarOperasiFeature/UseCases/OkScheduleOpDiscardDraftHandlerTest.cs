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

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkScheduleOpDiscardDraftHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private OkScheduleOpDiscardDraftHandler CreateSut()
    {
        return new OkScheduleOpDiscardDraftHandler(
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

    private static ScheduleOpModel CreateSampleScheduleOp(
        string scheduleOpId = "SCH-001",
        string orderOpId = "ORD-001",
        bool isDraft = true)
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
            [],
            isDraft: isDraft);
    }

    [Fact]
    public async Task Handle_GivenValidDraft_SuccessfullyDiscardsDraftAndResetsOpCaseSchedule()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 9, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", isDraft: true);
        opCase.ScheduleDraft(scheduleOp, new DateTime(2026, 10, 20, 7, 0, 0));

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        var cmd = new OkScheduleOpDiscardDraftCommand("ORD-001", "user1");
        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Requested);
        opCase.ScheduleOp.Should().Be(ScheduleOpReff.Default);

        _scheduleOpRepoMock.Verify(x => x.DeleteEntity(scheduleOp), Times.Once);
        _scheduleOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.Never);
        _opCaseRepoMock.Verify(x => x.SaveChanges(opCase), Times.Once);
        scheduleOp.ListHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GivenConfirmedSchedule_ThrowsInvalidOperationException()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 9, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", isDraft: false);
        opCase.Schedule(scheduleOp, new DateTime(2026, 10, 20, 7, 0, 0));

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        var cmd = new OkScheduleOpDiscardDraftCommand("ORD-001", "user1");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pelepasan draf hanya diizinkan untuk jadwal berstatus DRAFT*");

        _scheduleOpRepoMock.Verify(x => x.DeleteEntity(It.IsAny<IScheduleOpKey>()), Times.Never);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenScheduleNotFound_ThrowsKeyNotFoundException()
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

        var cmd = new OkScheduleOpDiscardDraftCommand("ORD-001", "user1");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Schedule Operasi tidak ditemukan.");

        _scheduleOpRepoMock.Verify(x => x.DeleteEntity(It.IsAny<IScheduleOpKey>()), Times.Never);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenEmptyOrderOpId_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new OkScheduleOpDiscardDraftCommand("", "user1");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_GivenEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new OkScheduleOpDiscardDraftCommand("ORD-001", "");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
