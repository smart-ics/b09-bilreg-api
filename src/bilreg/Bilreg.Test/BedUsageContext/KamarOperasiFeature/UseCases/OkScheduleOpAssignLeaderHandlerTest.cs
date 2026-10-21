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

public class OkScheduleOpAssignLeaderHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private OkScheduleOpAssignLeaderHandler CreateSut()
    {
        return new OkScheduleOpAssignLeaderHandler(
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

    private static PpaType CreateSampleDoctor(string ppaId = "DOC-01", string ppaName = "dr. Bedah Sp.B", ProfesiType? profesi = null)
    {
        var satTugas = new SatTugasType("ST-1", "Spesialis Bedah", profesi ?? ProfesiType.Dokter);
        var ppaSatTugas = new PpaSatTugasType(satTugas, true);
        return new PpaType(
            ppaId,
            ppaName,
            "dr. B",
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_GivenEmptyOrNullAlasanPerubahan_ThrowsArgumentException(string? invalidReason)
    {
        // ARRANGE
        var sut = CreateSut();
        var cmd = new OkScheduleOpAssignLeaderCommand("ORD-001", "DOC-02", "user-01", invalidReason!);

        // ACT
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_GivenSameLeaderAsActive_ThrowsArgumentException()
    {
        // ARRANGE
        var now = new DateTime(2026, 10, 20, 9, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");
        var opCase = OpCaseModel.Create(orderOp, now);
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", "DOC-01");
        opCase.Schedule(scheduleOp, now);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(doctor));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));

        var scheduleView = new ScheduleOpView(
            "SCH-001",
            orderOp.Pasien,
            new OrderOpReff("ORD-001", now, "Herniotomi"),
            UrgencyLevelEnum.Elective,
            now,
            60,
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        var cmd = new OkScheduleOpAssignLeaderCommand("ORD-001", "DOC-01", "user-01", "Ganti ke dokter yang sama");
        var sut = CreateSut();

        // ACT
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Dokter Operator Utama baru sama dengan Dokter Operator Utama yang aktif.*");
    }

    [Fact]
    public async Task Handle_GivenValidNewLeader_SuccessfullyAssignsLeaderAndSavesHistoryDelta()
    {
        // ARRANGE
        var now = new DateTime(2026, 10, 20, 9, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var oldDoctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");
        var newDoctor = CreateSampleDoctor("DOC-02", "dr. Pengganti Sp.B");
        var opCase = OpCaseModel.Create(orderOp, now);
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", "DOC-01");
        opCase.Schedule(scheduleOp, now);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(oldDoctor));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-02")))
            .Returns(MayBe.From(newDoctor));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));

        var scheduleView = new ScheduleOpView(
            "SCH-001",
            orderOp.Pasien,
            new OrderOpReff("ORD-001", now, "Herniotomi"),
            UrgencyLevelEnum.Elective,
            now,
            60,
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        var cmd = new OkScheduleOpAssignLeaderCommand("ORD-001", "DOC-02", "user-01", "Dokter utama berhalangan hadir");
        var sut = CreateSut();

        // ACT
        await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        scheduleOp.TeamLead.PpaId.Should().Be("DOC-02");
        scheduleOp.ListHistory.Should().HaveCount(1);
        var hist = scheduleOp.ListHistory.First();
        hist.ChangeType.Should().Be("ASSIGN_LEADER");
        hist.Alasan.Should().Be("Dokter utama berhalangan hadir");
        hist.OldTeamLead.PpaId.Should().Be("DOC-01");
        hist.NewTeamLead.PpaId.Should().Be("DOC-02");

        _scheduleOpRepoMock.Verify(x => x.SaveChanges(scheduleOp), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(opCase), Times.Once);
    }
}
