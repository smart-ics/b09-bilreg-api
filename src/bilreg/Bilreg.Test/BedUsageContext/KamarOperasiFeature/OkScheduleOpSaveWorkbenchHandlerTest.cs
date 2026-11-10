using Bilreg.Application.AdmisiContext.PpaFeature;
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
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OkScheduleOpSaveWorkbenchHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IKamarRepo> _kamarRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<IOrderOpResourceRepo> _resourceRepoMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private readonly OkScheduleOpSaveWorkbenchHandler _sut;
    private readonly DateTime _now = new(2026, 11, 10, 8, 30, 0);

    public OkScheduleOpSaveWorkbenchHandlerTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(_now);

        _sut = new OkScheduleOpSaveWorkbenchHandler(
            _scheduleOpRepoMock.Object,
            _orderOpRepoMock.Object,
            _kamarRepoMock.Object,
            _ppaRepoMock.Object,
            _opCaseRepoMock.Object,
            _resourceRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object);
    }

    private static PpaType CreateDoctor(string ppaId, string name)
    {
        return new PpaType(
            ppaId,
            name,
            "-",
            SmfType.Default,
            GroupSpesialisType.Default,
            new List<PpaLayananType> { new(new LayananReff("L1", "Bedah"), true) },
            new List<PpaSatTugasType> { new(new SatTugasType("ST1", "Dokter", ProfesiType.Dokter), true) },
            new List<ContactType>()
        );
    }

    private static PpaType CreateNurse(string ppaId, string name)
    {
        return new PpaType(
            ppaId,
            name,
            "-",
            SmfType.Default,
            GroupSpesialisType.Default,
            new List<PpaLayananType> { new(new LayananReff("L1", "Bedah"), true) },
            new List<PpaSatTugasType> { new(new SatTugasType("ST2", "Perawat", ProfesiType.Perawat), true) },
            new List<ContactType>()
        );
    }

    private static OrderOpModel CreateOrderOp(string orderOpId, PpaType dokter)
    {
        var audit = AuditTrailType.Create("USR1", DateTime.Today);
        var pasien = new PasienReff("RM001", "Pasien Test", new DateOnly(1990, 1, 1), "L");
        var reg = new RegReff("REG001", "RM001", "Pasien Test");

        return new OrderOpModel(
            orderOpId,
            DateTime.Today,
            audit,
            pasien,
            reg,
            Icd10Type.Default,
            JenisOperasiType.Default,
            "Appendectomy",
            UrgencyLevelEnum.Elective,
            TarifType.Default.ToReff(),
            dokter.ToReff(),
            120,
            DateTime.Today.AddDays(1),
            "-"
        );
    }

    private static KamarType CreateKamar(string kamarId, string name)
    {
        return KamarType.Default with { KamarId = kamarId, KamarName = name };
    }

    private static ScheduleOpModel CreateSchedule(
        string scheduleOpId,
        OrderOpModel orderOp,
        KamarType kamar,
        PpaType leader,
        DateTime tglOp,
        int durasi,
        IEnumerable<ScheduleOpPpaType>? additionalPpas = null)
    {
        var listPpa = new List<ScheduleOpPpaType>
        {
            new(1, leader.ToReff(), ProfesiType.Dokter, GroupSpesialisType.Default)
        };
        if (additionalPpas != null)
        {
            listPpa.AddRange(additionalPpas);
        }

        return new ScheduleOpModel(
            scheduleOpId,
            DateTime.Today,
            AuditTrailType.Create("USR1", DateTime.Today),
            orderOp.ToReff(),
            orderOp.Pasien,
            orderOp.UrgencyLevel,
            durasi,
            tglOp,
            kamar.ToReff(),
            orderOp.Reg,
            leader.ToReff(),
            new DateTime(3000, 1, 1),
            new DateTime(3000, 1, 1),
            OpCaseStateEnum.Scheduled,
            listPpa
        );
    }

    [Fact]
    public async Task Handle_DraftSchedule_WhenTimeAndResourcesMutated_SucceedsWithoutReasonEnforcement()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var kamar2 = CreateKamar("OK2", "Kamar Bedah 2");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today); // Draft state: Requested (0)
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, DateTime.Today.AddHours(8), 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "OK2"))).Returns(MayBe.From(kamar2));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(p => p.PpaId == "D001"))).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH001"))).Returns(MayBe.From(schedule));

        _resourceRepoMock.Setup(x => x.ListData("ORD001")).Returns(new List<OrderOpResourceDto>());
        _resourceRepoMock.Setup(x => x.Reconcile("ORD001", It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), "USR1"))
            .Returns((string ordId, string schId, IEnumerable<OrderOpResourceDto> targets, string uid) =>
                targets.Select(t => { t.OrderOpId = ordId; t.ScheduleOpId = schId; return t; }).ToList());

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK2",
            Tgl: "2026-11-11",
            Jam: "10:00",
            Durasi: 90,
            UserId: "USR1",
            DokterOperatorId: "D001",
            AlasanReschedule: "", // Draft suppresses formal reason
            AlasanAssignLeader: "",
            Publish: false,
            Resources: new List<SaveWorkbenchResourceDto>
            {
                new("res-1", "BLOOD", "Pack Red Cell", 2, "Kolf", "Gol A+")
            }
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsDraft.Should().BeTrue();
        response.Rescheduled.Should().BeTrue();
        response.Published.Should().BeFalse();
        response.ResourceChanged.Should().BeTrue();
        response.Resources.Should().HaveCount(1);
        response.Resources![0].Name.Should().Be("Pack Red Cell");

        _scheduleOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.AtLeastOnce);
        _resourceRepoMock.Verify(x => x.Reconcile("ORD001", It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), "USR1"), Times.Once);
    }

    [Fact]
    public async Task Handle_OfficialSchedule_WhenRescheduledWithoutReason_ThrowsArgumentException()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var kamar2 = CreateKamar("OK2", "Kamar Bedah 2");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, DateTime.Today.AddHours(8), 60);

        opCase.Schedule(schedule, DateTime.Today); // Promoted to Scheduled (Official!)

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar2));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Scheduled, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK2",
            Tgl: "2026-11-11",
            Jam: "10:00",
            Durasi: 90,
            UserId: "USR1",
            DokterOperatorId: "D001",
            AlasanReschedule: "abc", // Less than 5 characters!
            Publish: false
        );

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Alasan perubahan jadwal wajib diisi minimal 5 karakter*");
    }

    [Fact]
    public async Task Handle_OfficialSchedule_WhenLeaderChangedWithoutReason_ThrowsArgumentException()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var doc2 = CreateDoctor("D002", "dr. Second");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        opCase.Schedule(schedule, DateTime.Today); // Official

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(p => p.PpaId == "D001"))).Returns(MayBe.From(doc1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(p => p.PpaId == "D002"))).Returns(MayBe.From(doc2));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Scheduled, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D002", // Changed leader
            AlasanReschedule: "", // Time did not change, so no reschedule reason needed
            AlasanAssignLeader: "gnt", // Too short!
            Publish: false
        );

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Alasan perubahan dokter operator wajib diisi minimal 5 karakter*");
    }

    [Fact]
    public async Task Handle_OfficialSchedule_WithSelectiveReasons_SucceedsAndSyncsOrderOp()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var doc2 = CreateDoctor("D002", "dr. Second");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        opCase.Schedule(schedule, DateTime.Today); // Official

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(p => p.PpaId == "D001"))).Returns(MayBe.From(doc1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(p => p.PpaId == "D002"))).Returns(MayBe.From(doc2));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Scheduled, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D002",
            AlasanReschedule: "", // Time did not change
            AlasanAssignLeader: "Dokter utama berhalangan hadir dinas",
            Publish: false
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsDraft.Should().BeFalse();
        response.Rescheduled.Should().BeFalse();
        response.LeaderChanged.Should().BeTrue();
        response.Published.Should().BeFalse();

        _orderOpRepoMock.Verify(x => x.SaveChanges(It.Is<OrderOpModel>(o => o.Dokter.PpaId == "D002")), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPublishIsTrue_PromotesDraftToOfficial()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today); // Draft
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(p => p.PpaId == "D001"))).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D001",
            Publish: true
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsDraft.Should().BeFalse();
        response.Published.Should().BeTrue();

        _opCaseRepoMock.Verify(x => x.SaveChanges(It.Is<OpCaseModel>(o => o.OrderOpState == OpCaseStateEnum.Scheduled)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResourcesNull_SkipsResourceReconciliation()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, DateTime.Today.AddHours(8), 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var existingResource = OrderOpResourceDto.Create("res-existing", "ORD001", "SCH001", "IMPLANT", "Plate Screw", 1, "Set", "", "USR1");
        _resourceRepoMock.Setup(x => x.ListData("ORD001")).Returns([existingResource]);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-11",
            Jam: "10:00",
            Durasi: 90, // Changed time
            UserId: "USR1",
            Resources: null // Legacy caller
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.ResourceChanged.Should().BeFalse();
        response.Resources.Should().HaveCount(1);
        response.Resources![0].Name.Should().Be("Plate Screw");

        _resourceRepoMock.Verify(x => x.Reconcile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResourcesEmptyArray_DeletesAllResources()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, DateTime.Today.AddHours(8), 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var existingResource = OrderOpResourceDto.Create("res-existing", "ORD001", "SCH001", "IMPLANT", "Plate Screw", 1, "Set", "", "USR1");
        _resourceRepoMock.Setup(x => x.ListData("ORD001")).Returns([existingResource]);
        _resourceRepoMock.Setup(x => x.Reconcile("ORD001", It.IsAny<string>(), It.Is<IEnumerable<OrderOpResourceDto>>(c => !c.Any()), "USR1"))
            .Returns(new List<OrderOpResourceDto>());

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            Resources: new List<SaveWorkbenchResourceDto>() // Empty collection
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.ResourceChanged.Should().BeTrue();
        response.Resources.Should().BeEmpty();

        _resourceRepoMock.Verify(x => x.Reconcile("ORD001", It.IsAny<string>(), It.Is<IEnumerable<OrderOpResourceDto>>(c => !c.Any()), "USR1"), Times.Once);
    }

    [Fact]
    public async Task Handle_CleanSaveNoOp_SkipsPersistenceAndSideEffects()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var existingResource = OrderOpResourceDto.Create("res-1", "ORD001", "SCH001", "BLOOD", "Pack Red Cell", 2, "Kolf", "A+", "USR1");
        _resourceRepoMock.Setup(x => x.ListData("ORD001")).Returns([existingResource]);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D001",
            Publish: false,
            Resources: new List<SaveWorkbenchResourceDto>
            {
                new("res-1", "BLOOD", "Pack Red Cell", 2, "Kolf", "A+")
            }
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Rescheduled.Should().BeFalse();
        response.LeaderChanged.Should().BeFalse();
        response.PpaChanged.Should().BeFalse();
        response.ResourceChanged.Should().BeFalse();
        response.Published.Should().BeFalse();

        _scheduleOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.Never);
        _resourceRepoMock.Verify(x => x.Reconcile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), It.IsAny<string>()), Times.Never);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PpaReconciliation_MaintainsDemotedLeaderWhenIncludedInList()
    {
        // Arrange
        var docOld = CreateDoctor("D001", "dr. Old Leader");
        var docNew = CreateDoctor("D002", "dr. New Leader");
        var nurse = CreateNurse("N001", "Nurse One");

        var orderOp = CreateOrderOp("ORD001", docOld);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);

        var existingPpas = new List<ScheduleOpPpaType>
        {
            new(2, nurse.ToReff(), ProfesiType.Perawat, GroupSpesialisType.Default)
        };
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, docOld, tglOp, 60, existingPpas);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(p => p.PpaId == "D001"))).Returns(MayBe.From(docOld));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(p => p.PpaId == "D002"))).Returns(MayBe.From(docNew));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(p => p.PpaId == "N001"))).Returns(MayBe.From(nurse));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, docOld.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        ScheduleOpModel? savedSchedule = null;
        _scheduleOpRepoMock.Setup(x => x.SaveChanges(It.Is<ScheduleOpModel>(s => !s.AuditTrail.IsVoided)))
            .Callback<ScheduleOpModel>(s => savedSchedule = s);

        // ListPpaIds retains docOld ("D001") and nurse ("N001") as supporting members
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D002",
            ListPpaIds: new List<string> { "D001", "N001" },
            Publish: false
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.LeaderChanged.Should().BeTrue();
        savedSchedule.Should().NotBeNull();
        savedSchedule!.TeamLead.PpaId.Should().Be("D002");
        savedSchedule.ListPpa.Should().Contain(p => p.Ppa.PpaId == "D001");
        savedSchedule.ListPpa.Should().Contain(p => p.Ppa.PpaId == "N001");
    }

    [Fact]
    public async Task Handle_ResourceAdd_WhenNewResourceAddedToExistingSchedule_ReconcilesAndReturnsPersisted()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var existingResource = OrderOpResourceDto.Create("res-1", "ORD001", "SCH001", "BLOOD", "Pack Red Cell", 2, "Kolf", "A+", "USR1");
        _resourceRepoMock.Setup(x => x.ListData("ORD001")).Returns([existingResource]);

        var persistedResources = new List<OrderOpResourceDto>
        {
            existingResource,
            OrderOpResourceDto.Create("res-2", "ORD001", "SCH001", "IMPLANT", "Plate 3.5mm", 1, "Pcs", "", "USR1")
        };
        _resourceRepoMock.Setup(x => x.Reconcile("ORD001", It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), "USR1"))
            .Returns(persistedResources);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D001",
            Resources: new List<SaveWorkbenchResourceDto>
            {
                new("res-1", "BLOOD", "Pack Red Cell", 2, "Kolf", "A+"),
                new("", "IMPLANT", "Plate 3.5mm", 1, "Pcs", "")
            }
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.ResourceChanged.Should().BeTrue();
        response.Resources.Should().HaveCount(2);
        response.Resources.Should().Contain(x => x.Name == "Plate 3.5mm");
        _resourceRepoMock.Verify(x => x.Reconcile("ORD001", It.IsAny<string>(), It.Is<IEnumerable<OrderOpResourceDto>>(c => c.Count() == 2), "USR1"), Times.Once);
    }

    [Fact]
    public async Task Handle_ResourceUpdate_WhenExistingResourceModified_ReconcilesAndReturnsUpdated()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var existingResource = OrderOpResourceDto.Create("res-1", "ORD001", "SCH001", "BLOOD", "Pack Red Cell", 2, "Kolf", "A+", "USR1");
        _resourceRepoMock.Setup(x => x.ListData("ORD001")).Returns([existingResource]);

        var updatedResource = OrderOpResourceDto.Create("res-1", "ORD001", "SCH001", "BLOOD", "Pack Red Cell", 4, "Kolf", "A+ Urgent", "USR1");
        _resourceRepoMock.Setup(x => x.Reconcile("ORD001", It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), "USR1"))
            .Returns([updatedResource]);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D001",
            Resources: new List<SaveWorkbenchResourceDto>
            {
                new("res-1", "BLOOD", "Pack Red Cell", 4, "Kolf", "A+ Urgent")
            }
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.ResourceChanged.Should().BeTrue();
        response.Resources.Should().HaveCount(1);
        response.Resources![0].Qty.Should().Be(4);
        response.Resources![0].Notes.Should().Be("A+ Urgent");
        _resourceRepoMock.Verify(x => x.Reconcile("ORD001", It.IsAny<string>(), It.Is<IEnumerable<OrderOpResourceDto>>(c => c.First().Qty == 4), "USR1"), Times.Once);
    }

    [Fact]
    public async Task Handle_ResourceDelete_WhenResourceRemovedFromCollection_ReconcilesTargetCollection()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var res1 = OrderOpResourceDto.Create("res-1", "ORD001", "SCH001", "BLOOD", "Pack Red Cell", 2, "Kolf", "A+", "USR1");
        var res2 = OrderOpResourceDto.Create("res-2", "ORD001", "SCH001", "IMPLANT", "Plate", 1, "Pcs", "", "USR1");
        _resourceRepoMock.Setup(x => x.ListData("ORD001")).Returns([res1, res2]);

        _resourceRepoMock.Setup(x => x.Reconcile("ORD001", It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), "USR1"))
            .Returns([res1]);

        // Target contains only res1 (res2 deleted)
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D001",
            Resources: new List<SaveWorkbenchResourceDto>
            {
                new("res-1", "BLOOD", "Pack Red Cell", 2, "Kolf", "A+")
            }
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.ResourceChanged.Should().BeTrue();
        response.Resources.Should().HaveCount(1);
        response.Resources![0].Id.Should().Be("res-1");
        _resourceRepoMock.Verify(x => x.Reconcile("ORD001", It.IsAny<string>(), It.Is<IEnumerable<OrderOpResourceDto>>(c => c.Count() == 1), "USR1"), Times.Once);
    }

    [Fact]
    public async Task Handle_ResourceIdentical_WhenPayloadMatchesExistingResources_ResourceChangedIsFalse()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var existingResource = OrderOpResourceDto.Create("res-1", "ORD001", "SCH001", "BLOOD", "Pack Red Cell", 2, "Kolf", "A+", "USR1");
        _resourceRepoMock.Setup(x => x.ListData("ORD001")).Returns([existingResource]);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D001",
            Resources: new List<SaveWorkbenchResourceDto>
            {
                new("res-1", "BLOOD", "Pack Red Cell", 2, "Kolf", "A+")
            }
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.ResourceChanged.Should().BeFalse();
        response.Resources.Should().HaveCount(1);
        _resourceRepoMock.Verify(x => x.Reconcile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OfficialSchedule_WhenOnlyResourcesMutated_DoesNotRequireRescheduleOrLeaderReason()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        opCase.Schedule(schedule, DateTime.Today); // Official (Scheduled)

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Scheduled, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        _resourceRepoMock.Setup(x => x.ListData("ORD001")).Returns(new List<OrderOpResourceDto>());
        var createdResource = OrderOpResourceDto.Create("res-new", "ORD001", "SCH001", "SPECIAL_EQUIPMENT", "C-Arm", 1, "Unit", "", "USR1");
        _resourceRepoMock.Setup(x => x.Reconcile("ORD001", It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), "USR1"))
            .Returns([createdResource]);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60, // Time NOT changed
            UserId: "USR1",
            DokterOperatorId: "D001", // Leader NOT changed
            AlasanReschedule: "", // Empty: should NOT throw because time didn't change!
            AlasanAssignLeader: "", // Empty: should NOT throw because leader didn't change!
            Resources: new List<SaveWorkbenchResourceDto>
            {
                new("", "SPECIAL_EQUIPMENT", "C-Arm", 1, "Unit", "")
            }
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsDraft.Should().BeFalse();
        response.Rescheduled.Should().BeFalse();
        response.LeaderChanged.Should().BeFalse();
        response.ResourceChanged.Should().BeTrue();
        response.Resources.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_PublishCleanDraft_WhenNoOtherDeltas_PromotesDraftToOfficialAndSetsPublishedTrue()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today); // Draft
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var existingResource = OrderOpResourceDto.Create("res-1", "ORD001", "SCH001", "BLOOD", "Pack Red Cell", 2, "Kolf", "A+", "USR1");
        _resourceRepoMock.Setup(x => x.ListData("ORD001")).Returns([existingResource]);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D001",
            Publish: true, // Publish requested on clean draft
            Resources: new List<SaveWorkbenchResourceDto>
            {
                new("res-1", "BLOOD", "Pack Red Cell", 2, "Kolf", "A+")
            }
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.IsDraft.Should().BeFalse();
        response.Published.Should().BeTrue();
        response.Rescheduled.Should().BeFalse();
        response.LeaderChanged.Should().BeFalse();
        response.ResourceChanged.Should().BeFalse();

        _opCaseRepoMock.Verify(x => x.SaveChanges(It.Is<OpCaseModel>(o => o.OrderOpState == OpCaseStateEnum.Scheduled)), Times.Once);
    }

    [Fact]
    public async Task Handle_PublishOfficialSchedule_WhenNoDeltas_ReturnsNoOpWithPublishedFalse()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        opCase.Schedule(schedule, DateTime.Today); // Already Official!

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Scheduled, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D001",
            Publish: true // Calling publish on an already official schedule with no other deltas
        );

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.IsDraft.Should().BeFalse();
        response.Published.Should().BeFalse("An already official schedule cannot be re-published");
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
        _scheduleOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResourceRepoThrows_TransactionAbortsAndThrows()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        _resourceRepoMock.Setup(x => x.ListData("ORD001")).Returns(new List<OrderOpResourceDto>());
        _resourceRepoMock.Setup(x => x.Reconcile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Resource owner mismatch"));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK2",
            Tgl: "2026-11-11",
            Jam: "10:00",
            Durasi: 90,
            UserId: "USR1",
            Resources: new List<SaveWorkbenchResourceDto>
            {
                new("res-hijacked", "BLOOD", "Pack Red Cell", 2, "Kolf", "")
            }
        );

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Resource owner mismatch*");

        _mediatorMock.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenScheduleRepoThrowsCollision_TransactionAbortsAndPropagates()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(doc1));

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        _scheduleOpRepoMock.Setup(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()))
            .Throws(new ScheduleCollisionException("Kamar operasi telah terisi"));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "10:00",
            Durasi: 60,
            UserId: "USR1",
            Resources: new List<SaveWorkbenchResourceDto>
            {
                new("", "BLOOD", "PRC", 1, "Kantong", "")
            }
        );

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ScheduleCollisionException>()
            .WithMessage("*Kamar operasi telah terisi*");

        _resourceRepoMock.Verify(x => x.Reconcile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSupportingPpaNotFound_ThrowsBeforeTransaction()
    {
        // Arrange
        var doc1 = CreateDoctor("D001", "dr. First");
        var orderOp = CreateOrderOp("ORD001", doc1);
        var kamar1 = CreateKamar("OK1", "Kamar Bedah 1");
        var opCase = OpCaseModel.Create(orderOp, DateTime.Today);
        var tglOp = new DateTime(2026, 11, 10, 8, 0, 0);
        var schedule = CreateSchedule("SCH001", orderOp, kamar1, doc1, tglOp, 60);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(orderOp));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>())).Returns(MayBe.From(opCase));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamar1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(p => p.PpaId == "D001"))).Returns(MayBe.From(doc1));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(p => p.PpaId == "NON_EXISTENT_PPA"))).Returns(MayBe<PpaType>.None);

        var scheduleView = new ScheduleOpView(schedule.ScheduleOpId, orderOp.Pasien, orderOp.ToReff(),
            orderOp.UrgencyLevel, schedule.TglOp, schedule.Durasi, doc1.ToReff(), kamar1.ToReff(),
            OpCaseStateEnum.Requested, false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>())).Returns([scheduleView]);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>())).Returns(MayBe.From(schedule));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD001",
            KamarId: "OK1",
            Tgl: "2026-11-10",
            Jam: "08:00",
            Durasi: 60,
            UserId: "USR1",
            DokterOperatorId: "D001",
            ListPpaIds: new List<string> { "NON_EXISTENT_PPA" },
            Resources: new List<SaveWorkbenchResourceDto>
            {
                new("", "BLOOD", "PRC", 1, "Kantong", "")
            }
        );

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*PPA ID NON_EXISTENT_PPA tidak ditemukan*");

        _scheduleOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.Never);
        _resourceRepoMock.Verify(x => x.Reconcile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<OrderOpResourceDto>>(), It.IsAny<string>()), Times.Never);
    }
}
