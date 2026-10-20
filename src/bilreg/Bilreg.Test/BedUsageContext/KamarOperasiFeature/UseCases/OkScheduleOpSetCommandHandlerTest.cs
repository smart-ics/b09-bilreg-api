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

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkScheduleOpSetCommandHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IKamarRepo> _kamarRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private OkScheduleOpSetCommandHandler CreateSut()
    {
        return new OkScheduleOpSetCommandHandler(
            _scheduleOpRepoMock.Object,
            _orderOpRepoMock.Object,
            _kamarRepoMock.Object,
            _ppaRepoMock.Object,
            _opCaseRepoMock.Object,
            _mediatorMock.Object,
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

    private static KamarType CreateSampleKamar(string kamarId = "KAMAR-1", string kamarName = "OK 1")
    {
        return new KamarType(
            kamarId,
            kamarName,
            new BangsalReff("B-01", "Gedung Bedah"),
            new KelasReff("K-01", "Kelas 1"));
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

    private static OpCaseModel CreateSampleOpCase(OrderOpModel orderOp)
    {
        return OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
    }

    private static ScheduleOpModel CreateSampleScheduleOp(
        string scheduleOpId = "SCH-001",
        string orderOpId = "ORD-001",
        string kamarId = "KAMAR-1",
        DateTime? tglOp = null,
        int durasi = 60,
        int turnoverBuffer = 30)
    {
        var opTime = tglOp ?? new DateTime(2026, 10, 20, 8, 0, 0);
        return new ScheduleOpModel(
            scheduleOpId,
            new DateTime(2026, 10, 20),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 20)),
            new OrderOpReff(orderOpId, new DateTime(2026, 10, 20), "Herniotomi"),
            new PasienReff("PAS-001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            UrgencyLevelEnum.Elective,
            durasi,
            opTime,
            new KamarReff(kamarId, "OK 1"),
            new RegReff("REG-001", "PAS-001", "Pasien A"),
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            opTime,
            opTime.AddMinutes(durasi),
            OpCaseStateEnum.Scheduled,
            [new ScheduleOpPpaType(1, new PpaReff("DOC-01", "dr. Bedah Sp.B"), ProfesiType.Dokter, GroupSpesialisType.Default)],
            "",
            turnoverBuffer,
            []);
    }

    [Fact]
    public async Task Handle_NewSchedule_Success()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1", "OK 1");
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");
        var opCase = CreateSampleOpCase(orderOp);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KAMAR-1")))
            .Returns(MayBe.From(kamar));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(doctor));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView>());
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForDoctor("DOC-01", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());

        var cmd = new OkScheduleOpSetCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user1",
            DokterOperatorId: "DOC-01", TurnoverBuffer: 30);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ScheduleOpId.Should().NotBeNullOrWhiteSpace();

        _scheduleOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Once);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<OkScheduleOpSetEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RescheduleInPlace_Success()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1", "OK 1");
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");
        var opCase = CreateSampleOpCase(orderOp);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", new DateTime(2026, 10, 20, 8, 0, 0), 60);

        opCase.Schedule(existingSchedule, new DateTime(2026, 10, 20, 6, 0, 0));

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KAMAR-1")))
            .Returns(MayBe.From(kamar));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(doctor));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));

        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(existingSchedule));
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForDoctor("DOC-01", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());

        var cmd = new OkScheduleOpSetCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "10:00", 90, "user1",
            DokterOperatorId: "DOC-01", AlasanPerubahan: "Pergeseran jadwal klinis");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ScheduleOpId.Should().Be("SCH-001");

        existingSchedule.TglOp.Should().Be(new DateTime(2026, 10, 20, 10, 0, 0));
        existingSchedule.Durasi.Should().Be(90);
        existingSchedule.ListHistory.Should().HaveCount(1);
        existingSchedule.ListHistory.First().Alasan.Should().Be("Pergeseran jadwal klinis");

        _scheduleOpRepoMock.Verify(x => x.SaveChanges(existingSchedule), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(opCase), Times.Once);
    }

    [Fact]
    public async Task Handle_Reschedule_WithoutReason_ThrowsArgumentException()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1", "OK 1");
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");
        var opCase = CreateSampleOpCase(orderOp);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1");

        opCase.Schedule(existingSchedule, new DateTime(2026, 10, 20, 6, 0, 0));

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KAMAR-1")))
            .Returns(MayBe.From(kamar));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(doctor));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(existingSchedule));

        var cmd = new OkScheduleOpSetCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "10:00", 90, "user1",
            DokterOperatorId: "DOC-01", AlasanPerubahan: "");

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Alasan perubahan jadwal wajib diisi*");
    }

    [Fact]
    public async Task Handle_HardCollision_ThrowsScheduleCollisionException()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1", "OK 1");
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");
        var opCase = CreateSampleOpCase(orderOp);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KAMAR-1")))
            .Returns(MayBe.From(kamar));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(doctor));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView>());

        // Another active schedule in KAMAR-1 from 08:30 to 09:30
        var collidingScheduleView = new ScheduleOpView(
            "SCH-OTHER",
            new PasienReff("PAS-002", "Pasien B", new DateOnly(1985, 5, 5), "P"),
            new OrderOpReff("ORD-002", new DateTime(2026, 10, 20), "Apendiktomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 30, 0),
            60,
            new PpaReff("DOC-02", "dr. Lain Sp.B"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView> { collidingScheduleView });

        // Command tries to book 08:00 to 09:00 (overlaps with 08:30-09:30)
        var cmd = new OkScheduleOpSetCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user1",
            DokterOperatorId: "DOC-01");

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ScheduleCollisionException>()
            .WithMessage("*Bentrok jadwal kamar operasi: Kamar KAMAR-1 telah terisi oleh jadwal lain dari jam 08:30 s/d 09:30.*");
    }

    [Fact]
    public async Task Handle_DoctorOverlap_WithoutOverride_ThrowsScheduleWarningException()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1", "OK 1");
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");
        var opCase = CreateSampleOpCase(orderOp);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KAMAR-1")))
            .Returns(MayBe.From(kamar));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(doctor));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView>());
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());

        // Same doctor is operating in KAMAR-2 from 08:30 to 09:30
        var doctorScheduleInOtherRoom = new ScheduleOpView(
            "SCH-DOC-OTHER",
            new PasienReff("PAS-003", "Pasien C", new DateOnly(1980, 1, 1), "L"),
            new OrderOpReff("ORD-003", new DateTime(2026, 10, 20), "Laparotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 30, 0),
            60,
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            new KamarReff("KAMAR-2", "OK 2"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForDoctor("DOC-01", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView> { doctorScheduleInOtherRoom });

        var cmd = new OkScheduleOpSetCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user1",
            DokterOperatorId: "DOC-01", IsOverride: false);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ScheduleWarningException>();
        ex.Which.Warnings.Should().Contain(w => w.Contains("dr. Bedah Sp.B") && w.Contains("KAMAR-2"));
    }

    [Fact]
    public async Task Handle_DoctorOverlap_WithOverrideAndReason_Succeeds()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1", "OK 1");
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");
        var opCase = CreateSampleOpCase(orderOp);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KAMAR-1")))
            .Returns(MayBe.From(kamar));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(doctor));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView>());
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());

        var doctorScheduleInOtherRoom = new ScheduleOpView(
            "SCH-DOC-OTHER",
            new PasienReff("PAS-003", "Pasien C", new DateOnly(1980, 1, 1), "L"),
            new OrderOpReff("ORD-003", new DateTime(2026, 10, 20), "Laparotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 30, 0),
            60,
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            new KamarReff("KAMAR-2", "OK 2"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForDoctor("DOC-01", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView> { doctorScheduleInOtherRoom });

        var cmd = new OkScheduleOpSetCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user1",
            DokterOperatorId: "DOC-01", IsOverride: true, OverrideReason: "Disetujui kepala bedah untuk asistensi simultan");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ScheduleOpId.Should().NotBeNullOrWhiteSpace();
        _scheduleOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TurnoverBufferViolation_WithoutOverride_ThrowsScheduleWarningException()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1", "OK 1");
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");
        var opCase = CreateSampleOpCase(orderOp);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KAMAR-1")))
            .Returns(MayBe.From(kamar));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(doctor));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView>());
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForDoctor("DOC-01", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());

        // Prior schedule in KAMAR-1 finishes at 08:00
        var priorSchedule = new ScheduleOpView(
            "SCH-PRIOR",
            new PasienReff("PAS-002", "Pasien B", new DateOnly(1985, 5, 5), "P"),
            new OrderOpReff("ORD-002", new DateTime(2026, 10, 20), "Apendiktomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 7, 0, 0),
            60, // Ends at 08:00
            new PpaReff("DOC-02", "dr. Lain Sp.B"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView> { priorSchedule });

        // Next schedule starts at 08:15 (gap is 15 minutes, turnover buffer is 30)
        var cmd = new OkScheduleOpSetCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:15", 60, "user1",
            DokterOperatorId: "DOC-01", TurnoverBuffer: 30, IsOverride: false);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ScheduleWarningException>();
        ex.Which.Warnings.Should().Contain(w => w.Contains("turnover buffer 30 menit") && w.Contains("08:00"));
    }

    [Fact]
    public async Task Handle_Warning_WithOverride_WithoutOverrideReason_ThrowsArgumentException()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1", "OK 1");
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");
        var opCase = CreateSampleOpCase(orderOp);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KAMAR-1")))
            .Returns(MayBe.From(kamar));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(doctor));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView>());
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView>());

        var doctorScheduleInOtherRoom = new ScheduleOpView(
            "SCH-DOC-OTHER",
            new PasienReff("PAS-003", "Pasien C", new DateOnly(1980, 1, 1), "L"),
            new OrderOpReff("ORD-003", new DateTime(2026, 10, 20), "Laparotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 30, 0),
            60,
            new PpaReff("DOC-01", "dr. Bedah Sp.B"),
            new KamarReff("KAMAR-2", "OK 2"),
            OpCaseStateEnum.Scheduled,
            false);

        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForDoctor("DOC-01", new DateTime(2026, 10, 20)))
            .Returns(new List<ScheduleOpView> { doctorScheduleInOtherRoom });

        var cmd = new OkScheduleOpSetCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user1",
            DokterOperatorId: "DOC-01", IsOverride: true, OverrideReason: "");

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Alasan diskresi override wajib diisi*");
    }

    [Fact]
    public async Task Handle_DokterOperatorNotDoctor_ThrowsArgumentException()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1", "OK 1");
        var nurse = CreateSampleDoctor("DOC-01", "Ns. Perawat", ProfesiType.Perawat);
        var opCase = CreateSampleOpCase(orderOp);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KAMAR-1")))
            .Returns(MayBe.From(kamar));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(nurse));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(opCase));

        var cmd = new OkScheduleOpSetCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user1",
            DokterOperatorId: "DOC-01");

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*PPA yang ditugaskan sebagai Operator Utama harus berprofesi Dokter.*");
    }
}
