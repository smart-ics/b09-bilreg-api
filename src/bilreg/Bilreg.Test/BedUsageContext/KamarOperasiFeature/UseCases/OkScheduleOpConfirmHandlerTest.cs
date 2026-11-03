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

public class OkScheduleOpConfirmHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private OkScheduleOpConfirmHandler CreateSut()
    {
        return new OkScheduleOpConfirmHandler(
            _scheduleOpRepoMock.Object,
            _opCaseRepoMock.Object,
            _mediatorMock.Object,
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
        bool isDraft = true,
        DateTime? opTime = null,
        int durasi = 60,
        string kamarId = "KAMAR-1")
    {
        var time = opTime ?? new DateTime(2026, 10, 20, 8, 0, 0);
        return new ScheduleOpModel(
            scheduleOpId,
            new DateTime(2026, 10, 20),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 20)),
            new OrderOpReff(orderOpId, new DateTime(2026, 10, 20), "Herniotomi"),
            new PasienReff("PAS-001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            UrgencyLevelEnum.Elective,
            durasi,
            time,
            new KamarReff(kamarId, "OK 1"),
            new RegReff("REG-001", "PAS-001", "Pasien A"),
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            time,
            time.AddMinutes(durasi),
            OpCaseStateEnum.Requested,
            [new ScheduleOpPpaType(1, new PpaReff("DOC-01", "dr. Bedah Sp.B"), ProfesiType.Dokter, GroupSpesialisType.Default)],
            "",
            30,
            [],
            isDraft: isDraft);
    }

    [Fact]
    public async Task Handle_Success_PromotesDraftToScheduled_SavesBothAggregates_AndPublishesConfirmedEvent()
    {
        // Arrange
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", isDraft: true);
        opCase.ScheduleDraft(scheduleOp, new DateTime(2026, 10, 20, 7, 0, 0));

        _opCaseRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", scheduleOp.TglOp.Date))
            .Returns(new List<ScheduleOpView>());

        var cmd = new OkScheduleOpConfirmCommand("ORD-001", "user-admin");
        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        scheduleOp.IsDraft.Should().BeFalse();
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);
        scheduleOp.AuditTrail.Modified.UserId.Should().Be("user-admin");
        scheduleOp.AuditTrail.Modified.Timestamp.Should().Be(now);

        _scheduleOpRepoMock.Verify(x => x.SaveChanges(scheduleOp), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(opCase), Times.Once);
        _mediatorMock.Verify(x => x.Publish(
            It.Is<OkScheduleOpConfirmedEvent>(e => e.Aggregate == scheduleOp && e.OccurredAt == now),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ScheduleAlreadyOfficial_ThrowsInvalidOperationException()
    {
        // Arrange
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
        // Schedule already official (isDraft = false)
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", isDraft: false);
        opCase.Schedule(scheduleOp, new DateTime(2026, 10, 20, 7, 0, 0));

        _opCaseRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        var cmd = new OkScheduleOpConfirmCommand("ORD-001", "user-admin");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah berstatus resmi*");

        _scheduleOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.Never);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<OkScheduleOpConfirmedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FinalZeroRoomCollision_ThrowsScheduleCollisionException()
    {
        // Arrange
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var opTime = new DateTime(2026, 10, 20, 8, 0, 0);
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", isDraft: true, opTime: opTime, durasi: 60, kamarId: "KAMAR-1");
        opCase.ScheduleDraft(scheduleOp, new DateTime(2026, 10, 20, 7, 0, 0));

        _opCaseRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        // Another schedule already in KAMAR-1 from 08:30 to 09:30 (overlaps 08:00 - 09:00)
        var collidingSchedule = new ScheduleOpView(
            "SCH-OTHER",
            new PasienReff("PAS-002", "Pasien B", new DateOnly(1985, 5, 5), "P"),
            new OrderOpReff("ORD-002", new DateTime(2026, 10, 20), "Apendiktomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 30, 0),
            60,
            new PpaReff("DOC-02", "dr. Lain"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", opTime.Date))
            .Returns(new List<ScheduleOpView> { collidingSchedule });

        var cmd = new OkScheduleOpConfirmCommand("ORD-001", "user-admin");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ScheduleCollisionException>()
            .WithMessage("*Bentrok jadwal kamar operasi: Kamar KAMAR-1 telah terisi*");

        _scheduleOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.Never);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<OkScheduleOpConfirmedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SelfScheduleInRoomCollisionCheck_DoesNotTriggerCollision()
    {
        // Arrange
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var opTime = new DateTime(2026, 10, 20, 8, 0, 0);
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", isDraft: true, opTime: opTime, durasi: 60, kamarId: "KAMAR-1");
        opCase.ScheduleDraft(scheduleOp, new DateTime(2026, 10, 20, 7, 0, 0));

        _opCaseRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        // The query returns the self draft schedule
        var selfSchedule = new ScheduleOpView(
            "SCH-001",
            new PasienReff("PAS-001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            new OrderOpReff("ORD-001", new DateTime(2026, 10, 20), "Herniotomi"),
            UrgencyLevelEnum.Elective,
            opTime,
            60,
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Requested,
            false,
            IsDraft: true);

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", opTime.Date))
            .Returns(new List<ScheduleOpView> { selfSchedule });

        var cmd = new OkScheduleOpConfirmCommand("ORD-001", "user-admin");
        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        scheduleOp.IsDraft.Should().BeFalse();
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);
        _scheduleOpRepoMock.Verify(x => x.SaveChanges(scheduleOp), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(opCase), Times.Once);
    }

    [Fact]
    public async Task Handle_VoidScheduleInRoom_DoesNotTriggerCollision()
    {
        // Arrange
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var opTime = new DateTime(2026, 10, 20, 8, 0, 0);
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", isDraft: true, opTime: opTime, durasi: 60, kamarId: "KAMAR-1");
        opCase.ScheduleDraft(scheduleOp, new DateTime(2026, 10, 20, 7, 0, 0));

        _opCaseRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        // Overlapping schedule but is voided
        var voidSchedule = new ScheduleOpView(
            "SCH-VOID",
            new PasienReff("PAS-002", "Pasien B", new DateOnly(1985, 5, 5), "P"),
            new OrderOpReff("ORD-002", new DateTime(2026, 10, 20), "Apendiktomi"),
            UrgencyLevelEnum.Elective,
            opTime,
            60,
            new PpaReff("DOC-02", "dr. Lain"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Cancelled,
            IsVoid: true);

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", opTime.Date))
            .Returns(new List<ScheduleOpView> { voidSchedule });

        var cmd = new OkScheduleOpConfirmCommand("ORD-001", "user-admin");
        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        scheduleOp.IsDraft.Should().BeFalse();
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);
    }

    [Fact]
    public async Task Handle_OpCaseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe<OpCaseModel>.None);

        var cmd = new OkScheduleOpConfirmCommand("ORD-NOTFOUND", "user-admin");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*OpCase untuk Order Operasi ID ORD-NOTFOUND tidak ditemukan.*");
    }

    [Fact]
    public async Task Handle_ScheduleOpNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001");
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));

        _opCaseRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView>());

        var cmd = new OkScheduleOpConfirmCommand("ORD-001", "user-admin");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Jadwal operasi untuk Order Operasi ID ORD-001 tidak ditemukan.*");
    }

    [Fact]
    public async Task Handle_ScheduleOpFoundViaPasienListData_WhenOpCaseScheduleOpUnset_Success()
    {
        // Arrange
        var now = new DateTime(2026, 10, 20, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001");
        // opCase created without ScheduleOp set yet
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", isDraft: true);

        _opCaseRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(opCase));

        var view = new ScheduleOpView(
            "SCH-001",
            orderOp.Pasien,
            new OrderOpReff("ORD-001", new DateTime(2026, 10, 20), "Herniotomi"),
            UrgencyLevelEnum.Elective,
            scheduleOp.TglOp,
            60,
            new PpaReff("DOC-01", "dr. Bedah"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Requested,
            false,
            IsDraft: true);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.Is<IPasienKey>(p => p.PasienId == "PAS-001")))
            .Returns(new List<ScheduleOpView> { view });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", scheduleOp.TglOp.Date))
            .Returns(new List<ScheduleOpView>());

        var cmd = new OkScheduleOpConfirmCommand("ORD-001", "user-admin");
        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        scheduleOp.IsDraft.Should().BeFalse();
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);
        _scheduleOpRepoMock.Verify(x => x.SaveChanges(scheduleOp), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(opCase), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyOrderOpId_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new OkScheduleOpConfirmCommand("", "user-admin");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_EmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new OkScheduleOpConfirmCommand("ORD-001", "");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
