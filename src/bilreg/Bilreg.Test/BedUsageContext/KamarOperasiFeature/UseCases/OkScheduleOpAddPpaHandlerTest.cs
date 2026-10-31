using Ardalis.GuardClauses;
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
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkScheduleOpAddPpaHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private OkScheduleOpAddPpaHandler CreateSut()
    {
        return new OkScheduleOpAddPpaHandler(
            _scheduleOpRepoMock.Object,
            _orderOpRepoMock.Object,
            _ppaRepoMock.Object,
            _opCaseRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    private static OrderOpModel CreateSampleOrderOp(string orderOpId = "ORD-001", string dokterId = "DOC-01")
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
            new PpaReff(dokterId, "dr. Bedah Sp.B"),
            60,
            new DateTime(2026, 10, 20),
            "-",
            new PpaReff(dokterId, "dr. Bedah Sp.B"),
            new LayananReff("LYN-01", "Klinik Bedah"),
            "",
            KlasifikasiOperasiEnum.Sedang,
            AnestesiPlanEnum.General,
            PostOpCareUnitEnum.PACU,
            "",
            []);
    }

    private static PpaType CreateSamplePpa(string ppaId = "PPA-02", string ppaName = "dr. Anestesi Sp.An", ProfesiType? profesi = null)
    {
        var satTugas = new SatTugasType("ST-2", "Spesialis Anestesi", profesi ?? ProfesiType.Dokter);
        var ppaSatTugas = new PpaSatTugasType(satTugas, true);
        return new PpaType(
            ppaId,
            ppaName,
            "dr. A",
            SmfType.Default,
            GroupSpesialisType.Default,
            [],
            [ppaSatTugas],
            []);
    }

    private static ScheduleOpModel CreateSampleScheduleOp(
        string scheduleOpId = "SCH-001",
        string orderOpId = "ORD-001",
        string leadDoctorId = "DOC-01")
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
            new PpaReff(leadDoctorId, "dr. Bedah Sp.B"),
            opTime,
            opTime.AddMinutes(60),
            OpCaseStateEnum.Scheduled,
            [new ScheduleOpPpaType(1, new PpaReff(leadDoctorId, "dr. Bedah Sp.B"), ProfesiType.Dokter, GroupSpesialisType.Default)],
            "",
            30,
            []);
    }

    [Fact]
    public async Task Handle_GivenNoCollision_SuccessfullyAddsPpa()
    {
        // ARRANGE
        var now = new DateTime(2026, 10, 20, 7, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var ppa = CreateSamplePpa("DOC-02", "dr. Anestesi Sp.An");
        var opCase = OpCaseModel.Create(orderOp, now);
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", "DOC-01");
        opCase.Schedule(scheduleOp, now);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-02")))
            .Returns(MayBe.From(ppa));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));

        var scheduleView = new ScheduleOpView(
            "SCH-001",
            orderOp.Pasien,
            new OrderOpReff("ORD-001", now, "Herniotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 0, 0),
            60,
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForDoctor("DOC-02", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForPpaInTeam("DOC-02", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());

        var cmd = new OkScheduleOpAddPpaCommand("ORD-001", "DOC-02", "user-01");
        var sut = CreateSut();

        // ACT
        await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        scheduleOp.ListPpa.Should().HaveCount(2);
        scheduleOp.ListPpa.Should().Contain(x => x.Ppa.PpaId == "DOC-02");
        _scheduleOpRepoMock.Verify(x => x.SaveChanges(scheduleOp), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(opCase), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenPpaCollision_WithoutOverride_ThrowsScheduleWarningException()
    {
        // ARRANGE
        var now = new DateTime(2026, 10, 20, 7, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var ppa = CreateSamplePpa("DOC-02", "dr. Anestesi Sp.An");
        var opCase = OpCaseModel.Create(orderOp, now);
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", "DOC-01");
        opCase.Schedule(scheduleOp, now);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-02")))
            .Returns(MayBe.From(ppa));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));

        var scheduleView = new ScheduleOpView(
            "SCH-001",
            orderOp.Pasien,
            new OrderOpReff("ORD-001", now, "Herniotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 0, 0),
            60,
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        // PPA is scheduled in another room KAMAR-2 as team member
        var collidingSchedule = new ScheduleOpView(
            "SCH-OTHER",
            new PasienReff("PAS-002", "Pasien B", new DateOnly(1985, 5, 5), "P"),
            new OrderOpReff("ORD-002", new DateTime(2026, 10, 20), "Apendiktomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 30, 0),
            60,
            new PpaReff("DOC-99", "dr. Lain"),
            new KamarReff("KAMAR-2", "OK 2"),
            OpCaseStateEnum.Scheduled,
            false,
            ProfesiId: "PROF-02",
            ProfesiName: "Dokter Anestesi");

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForDoctor("DOC-02", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForPpaInTeam("DOC-02", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView> { collidingSchedule });

        var cmd = new OkScheduleOpAddPpaCommand("ORD-001", "DOC-02", "user-01", IsOverride: false);
        var sut = CreateSut();

        // ACT
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        var ex = await act.Should().ThrowAsync<ScheduleWarningException>();
        ex.Which.Warnings.Should().Contain(w =>
            w.Contains("dr. Anestesi Sp.An") &&
            w.Contains("sebagai Dokter Anestesi") &&
            w.Contains("KAMAR-2"));
    }

    [Fact]
    public async Task Handle_GivenPpaCollision_WithOverrideAndReason_SucceedsWithoutHistEntry()
    {
        // ARRANGE
        var now = new DateTime(2026, 10, 20, 7, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var ppa = CreateSamplePpa("DOC-02", "dr. Anestesi Sp.An");
        var opCase = OpCaseModel.Create(orderOp, now);
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", "DOC-01");
        opCase.Schedule(scheduleOp, now);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-02")))
            .Returns(MayBe.From(ppa));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));

        var scheduleView = new ScheduleOpView(
            "SCH-001",
            orderOp.Pasien,
            new OrderOpReff("ORD-001", now, "Herniotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 0, 0),
            60,
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        var collidingSchedule = new ScheduleOpView(
            "SCH-OTHER",
            new PasienReff("PAS-002", "Pasien B", new DateOnly(1985, 5, 5), "P"),
            new OrderOpReff("ORD-002", new DateTime(2026, 10, 20), "Apendiktomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 30, 0),
            60,
            new PpaReff("DOC-99", "dr. Lain"),
            new KamarReff("KAMAR-2", "OK 2"),
            OpCaseStateEnum.Scheduled,
            false,
            ProfesiId: "PROF-02",
            ProfesiName: "Dokter Anestesi");

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForDoctor("DOC-02", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForPpaInTeam("DOC-02", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView> { collidingSchedule });

        var cmd = new OkScheduleOpAddPpaCommand(
            "ORD-001", "DOC-02", "user-01",
            IsOverride: true, OverrideReason: "Dokter Anestesi standby di 2 kamar berdekatan");
        var sut = CreateSut();

        // ACT
        await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        scheduleOp.ListPpa.Should().HaveCount(2);
        scheduleOp.ListPpa.Should().Contain(x => x.Ppa.PpaId == "DOC-02");
        scheduleOp.ListHistory.Should().BeEmpty("Penambahan PPA dengan override tidak boleh mencatat entri histori ke BILRG_ScheduleOpHist");

        _scheduleOpRepoMock.Verify(x => x.SaveChanges(scheduleOp), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(opCase), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenPpaCollision_WithOverrideTrueButEmptyReason_ThrowsArgumentException()
    {
        // ARRANGE
        var now = new DateTime(2026, 10, 20, 7, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var ppa = CreateSamplePpa("DOC-02", "dr. Anestesi Sp.An");
        var opCase = OpCaseModel.Create(orderOp, now);
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", "DOC-01");
        opCase.Schedule(scheduleOp, now);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-02")))
            .Returns(MayBe.From(ppa));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));

        var scheduleView = new ScheduleOpView(
            "SCH-001",
            orderOp.Pasien,
            new OrderOpReff("ORD-001", now, "Herniotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 0, 0),
            60,
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        var collidingSchedule = new ScheduleOpView(
            "SCH-OTHER",
            new PasienReff("PAS-002", "Pasien B", new DateOnly(1985, 5, 5), "P"),
            new OrderOpReff("ORD-002", new DateTime(2026, 10, 20), "Apendiktomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 30, 0),
            60,
            new PpaReff("DOC-99", "dr. Lain"),
            new KamarReff("KAMAR-2", "OK 2"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForDoctor("DOC-02", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView> { collidingSchedule });
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForPpaInTeam("DOC-02", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());

        var cmd = new OkScheduleOpAddPpaCommand(
            "ORD-001", "DOC-02", "user-01",
            IsOverride: true, OverrideReason: "   ");
        var sut = CreateSut();

        // ACT
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Alasan diskresi override wajib diisi*");
    }
}
