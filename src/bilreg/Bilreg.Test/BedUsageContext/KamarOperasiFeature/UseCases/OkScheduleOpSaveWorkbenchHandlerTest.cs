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

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkScheduleOpSaveWorkbenchHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IKamarRepo> _kamarRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private OkScheduleOpSaveWorkbenchHandler CreateSut()
    {
        return new OkScheduleOpSaveWorkbenchHandler(
            _scheduleOpRepoMock.Object,
            _orderOpRepoMock.Object,
            _kamarRepoMock.Object,
            _ppaRepoMock.Object,
            _opCaseRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object);
    }

    private static OrderOpModel CreateSampleOrderOp(string orderOpId = "ORD-001", string dokterId = "DOC-01", bool isVoid = false)
    {
        var auditTrail = AuditTrailType.Create("u1", new DateTime(2026, 10, 20));
        if (isVoid)
        {
            auditTrail.Batal("u1", new DateTime(2026, 10, 20));
        }

        return new OrderOpModel(
            orderOpId,
            new DateTime(2026, 10, 20),
            auditTrail,
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

    private static PpaType CreateSampleNurse(string ppaId = "NURSE-01", string ppaName = "Ns. Perawat")
    {
        var satTugas = new SatTugasType("ST-2", "Perawat Bedah", ProfesiType.Perawat);
        var ppaSatTugas = new PpaSatTugasType(satTugas, true);
        return new PpaType(
            ppaId,
            ppaName,
            "Ns. P",
            SmfType.Default,
            GroupSpesialisType.Default,
            [],
            [ppaSatTugas],
            []);
    }

    private static OpCaseModel CreateSampleOpCase(OrderOpModel orderOp, OpCaseStateEnum state = OpCaseStateEnum.Scheduled)
    {
        var opCase = OpCaseModel.Create(orderOp, new DateTime(2026, 10, 20));
        if (state == OpCaseStateEnum.Cancelled)
        {
            opCase.CancelOrder(new DateTime(2026, 10, 20));
        }
        else if (state == OpCaseStateEnum.OpStarted)
        {
            opCase.Start(new DateTime(2026, 10, 20, 8, 30, 0));
        }
        return opCase;
    }

    private static ScheduleOpModel CreateSampleScheduleOp(
        string scheduleOpId = "SCH-001",
        string orderOpId = "ORD-001",
        string kamarId = "KAMAR-1",
        DateTime? tglOp = null,
        int durasi = 60,
        int turnoverBuffer = 30,
        string teamLeadId = "DOC-01",
        string teamLeadName = "dr. Bedah Sp.B",
        bool isDraft = false,
        List<ScheduleOpPpaType>? listPpa = null)
    {
        var opTime = tglOp ?? new DateTime(2026, 10, 20, 8, 0, 0);
        var ppaList = listPpa ?? [new ScheduleOpPpaType(1, new PpaReff(teamLeadId, teamLeadName), ProfesiType.Dokter, GroupSpesialisType.Default)];

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
            new PpaReff(teamLeadId, teamLeadName),
            opTime,
            opTime.AddMinutes(durasi),
            isDraft ? OpCaseStateEnum.Requested : OpCaseStateEnum.Scheduled,
            ppaList,
            "",
            turnoverBuffer,
            [],
            isDraft: isDraft);
    }

    private static ScheduleOpView CreateSampleScheduleOpView(
        string scheduleOpId = "SCH-001",
        string orderOpId = "ORD-001",
        string kamarId = "KAMAR-1",
        DateTime? tglOp = null,
        int durasi = 60,
        int turnoverBuffer = 30,
        string teamLeadId = "DOC-01",
        string teamLeadName = "dr. Bedah Sp.B",
        bool isVoid = false,
        bool isDraft = false,
        string profesiName = "Spesialis Bedah")
    {
        var opTime = tglOp ?? new DateTime(2026, 10, 20, 8, 0, 0);
        return new ScheduleOpView(
            scheduleOpId,
            new PasienReff("PAS-001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            new OrderOpReff(orderOpId, new DateTime(2026, 10, 20), "Herniotomi"),
            UrgencyLevelEnum.Elective,
            opTime,
            durasi,
            new PpaReff(teamLeadId, teamLeadName),
            new KamarReff(kamarId, "OK 1"),
            isDraft ? OpCaseStateEnum.Requested : OpCaseStateEnum.Scheduled,
            isVoid,
            TurnoverBuffer: turnoverBuffer,
            ProfesiId: "ST-1",
            ProfesiName: profesiName,
            IsDraft: isDraft);
    }

    #region Input Validation & Preconditions

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_GivenEmptyOrderOpId_ThrowsArgumentException(string? orderOpId)
    {
        var sut = CreateSut();
        var cmd = new OkScheduleOpSaveWorkbenchCommand(orderOpId!, "KAMAR-1", "2026-10-20", "08:00", 60, "user-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_GivenEmptyKamarId_ThrowsArgumentException(string? kamarId)
    {
        var sut = CreateSut();
        var cmd = new OkScheduleOpSaveWorkbenchCommand("ORD-001", kamarId!, "2026-10-20", "08:00", 60, "user-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("20-10-2026")]
    [InlineData("invalid-date")]
    public async Task Handle_GivenInvalidDateFormat_ThrowsArgumentException(string tgl)
    {
        var sut = CreateSut();
        var cmd = new OkScheduleOpSaveWorkbenchCommand("ORD-001", "KAMAR-1", tgl, "08:00", 60, "user-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_GivenEmptyJam_ThrowsArgumentException(string? jam)
    {
        var sut = CreateSut();
        var cmd = new OkScheduleOpSaveWorkbenchCommand("ORD-001", "KAMAR-1", "2026-10-20", jam!, 60, "user-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Handle_GivenNegativeOrZeroDurasi_ThrowsArgumentException(int durasi)
    {
        var sut = CreateSut();
        var cmd = new OkScheduleOpSaveWorkbenchCommand("ORD-001", "KAMAR-1", "2026-10-20", "08:00", durasi, "user-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_GivenEmptyUserId_ThrowsArgumentException(string? userId)
    {
        var sut = CreateSut();
        var cmd = new OkScheduleOpSaveWorkbenchCommand("ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, userId!);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_GivenNonExistentOrderOp_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe<OrderOpModel>.None);

        var cmd = new OkScheduleOpSaveWorkbenchCommand("ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Order Operasi ID ORD-001 tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_GivenVoidedOrderOp_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01", isVoid: true);
        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));

        var cmd = new OkScheduleOpSaveWorkbenchCommand("ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*sudah dibatalkan/void*");
    }

    [Fact]
    public async Task Handle_GivenNonExistentKamar_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe<KamarType>.None);

        var cmd = new OkScheduleOpSaveWorkbenchCommand("ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Kamar Operasi ID KAMAR-1 tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_GivenCancelledOpCase_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Cancelled);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));

        var cmd = new OkScheduleOpSaveWorkbenchCommand("ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*telah dibatalkan*");
    }

    [Fact]
    public async Task Handle_GivenOpCaseStarted_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.OpStarted);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));

        var cmd = new OkScheduleOpSaveWorkbenchCommand("ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*tidak dapat dijadwalkan ulang karena operasi telah dimulai atau selesai*");
    }

    [Fact]
    public async Task Handle_GivenNoExistingSchedule_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Requested);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe<ScheduleOpModel>.None);
        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<PasienModel>())).Returns(new List<ScheduleOpView>());

        var cmd = new OkScheduleOpSaveWorkbenchCommand("ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Jadwal operasi untuk Order Operasi ID ORD-001 tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_GivenPublishTrueWhenAlreadyOfficial_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", isDraft: false);
        opCase.Schedule(existingSchedule);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01",
            Publish: true);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*sudah berstatus resmi (bukan draf)*");
    }

    [Fact]
    public async Task Handle_GivenTargetDoctorNotFound_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", isDraft: false);
        opCase.Schedule(existingSchedule);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe<PpaType>.None);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01",
            DokterOperatorId: "DOC-999");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Dokter Operator dengan ID DOC-999 tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_GivenTargetDoctorNotDokter_ThrowsArgumentException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", isDraft: false);
        opCase.Schedule(existingSchedule);
        var nurse = CreateSampleNurse("NURSE-01", "Ns. Perawat");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe.From(nurse));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01",
            DokterOperatorId: "NURSE-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Operator Utama harus berprofesi Dokter*");
    }

    #endregion

    #region Selective Reason Validation (Official Schedule vs Draft Schedule)

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_OfficialSchedule_WhenRescheduledWithoutReason_ThrowsArgumentException(string? invalidReason)
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), isDraft: false);
        opCase.Schedule(existingSchedule);
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe.From(doctor));

        // Reschedule changed: 08:00 -> 10:00, but reason is missing
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "10:00", 60, "user-01",
            DokterOperatorId: "DOC-01",
            AlasanReschedule: invalidReason!);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Alasan perubahan jadwal (reschedule) wajib diisi*");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("1234")]
    public async Task Handle_OfficialSchedule_WhenRescheduledWithReasonLessThan5Chars_ThrowsArgumentException(string shortReason)
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), isDraft: false);
        opCase.Schedule(existingSchedule);
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe.From(doctor));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "10:00", 60, "user-01",
            DokterOperatorId: "DOC-01",
            AlasanReschedule: shortReason);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*minimal 5 karakter*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_OfficialSchedule_WhenLeaderChangedWithoutReason_ThrowsArgumentException(string? invalidReason)
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), teamLeadId: "DOC-01", isDraft: false);
        opCase.Schedule(existingSchedule);
        var newDoctor = CreateSampleDoctor("DOC-02", "dr. Baru Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<PpaType>(p => p.PpaId == "DOC-02"))).Returns(MayBe.From(newDoctor));

        // Time unchanged: 08:00, leader changed: DOC-01 -> DOC-02, but leader reason is missing
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01",
            DokterOperatorId: "DOC-02",
            AlasanAssignLeader: invalidReason!);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Alasan pergantian dokter operator utama wajib diisi*");
    }

    [Theory]
    [InlineData("gnt")]
    [InlineData("1234")]
    public async Task Handle_OfficialSchedule_WhenLeaderChangedWithReasonLessThan5Chars_ThrowsArgumentException(string shortReason)
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), teamLeadId: "DOC-01", isDraft: false);
        opCase.Schedule(existingSchedule);
        var newDoctor = CreateSampleDoctor("DOC-02", "dr. Baru Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<PpaType>(p => p.PpaId == "DOC-02"))).Returns(MayBe.From(newDoctor));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01",
            DokterOperatorId: "DOC-02",
            AlasanAssignLeader: shortReason);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*minimal 5 karakter*");
    }

    [Fact]
    public async Task Handle_OfficialSchedule_WhenOnlyLeaderChanges_RescheduleReasonNotRequired_Success()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), teamLeadId: "DOC-01", isDraft: false);
        opCase.Schedule(existingSchedule);
        var newDoctor = CreateSampleDoctor("DOC-02", "dr. Baru Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<PpaType>(p => p.PpaId == "DOC-02"))).Returns(MayBe.From(newDoctor));

        // Time unchanged (08:00, KAMAR-1, 60m), AlasanReschedule is empty, AlasanAssignLeader is valid
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:00", 60, "user-01",
            DokterOperatorId: "DOC-02",
            AlasanReschedule: "",
            AlasanAssignLeader: "Dokter berhalangan karena seminar");

        var response = await sut.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        response.LeaderChanged.Should().BeTrue();
        response.Rescheduled.Should().BeFalse();
        response.IsDraft.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_OfficialSchedule_WhenOnlyTimeChanges_LeaderReasonNotRequired_Success()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), teamLeadId: "DOC-01", isDraft: false);
        opCase.Schedule(existingSchedule);
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<PpaType>(p => p.PpaId == "DOC-01"))).Returns(MayBe.From(doctor));

        // Time changed: 08:00 -> 10:00, Leader unchanged, AlasanAssignLeader is empty, AlasanReschedule is valid
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "10:00", 60, "user-01",
            DokterOperatorId: "DOC-01",
            AlasanReschedule: "Kamar operasi disterilisasi ulang",
            AlasanAssignLeader: "");

        var response = await sut.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        response.LeaderChanged.Should().BeFalse();
        response.Rescheduled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DraftSchedule_WhenMutationsOccur_ReasonsAreOptional_Success()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Requested);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), teamLeadId: "DOC-01", isDraft: true);
        opCase.ScheduleDraft(existingSchedule);
        var newDoctor = CreateSampleDoctor("DOC-02", "dr. Baru Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<PpaType>(p => p.PpaId == "DOC-02"))).Returns(MayBe.From(newDoctor));

        // Time changed AND leader changed on draft: both reasons empty -> should succeed!
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "11:00", 90, "user-01",
            DokterOperatorId: "DOC-02",
            AlasanReschedule: "",
            AlasanAssignLeader: "");

        var response = await sut.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        response.IsDraft.Should().BeTrue();
        response.Rescheduled.Should().BeTrue();
        response.LeaderChanged.Should().BeTrue();
        response.Published.Should().BeFalse();
    }

    #endregion

    #region Zero Room Collision (Hard Block HTTP 409)

    [Fact]
    public async Task Handle_WhenRoomHasOverlappingSchedule_ThrowsScheduleCollisionException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), isDraft: false);
        opCase.Schedule(existingSchedule);
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe.From(doctor));

        // Active schedule in KAMAR-1: 09:00 - 10:30 (SCH-OTHER)
        var collidingSchedule = CreateSampleScheduleOpView("SCH-OTHER", "ORD-999", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 9, 0, 0), durasi: 90);
        _scheduleOpRepoMock
            .Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", new DateTime(2026, 10, 20)))
            .Returns([collidingSchedule]);

        // Attempting to schedule at 09:30 - 10:30 (overlaps with 09:00 - 10:30)
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "09:30", 60, "user-01",
            DokterOperatorId: "DOC-01",
            AlasanReschedule: "Pindah jam operasi",
            IsOverride: true); // Even with override, room collision must HARD BLOCK

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ScheduleCollisionException>()
            .WithMessage("*Bentrok jadwal kamar operasi: Kamar KAMAR-1 telah terisi*");
    }

    [Fact]
    public async Task Handle_WhenRoomHasOverlappingScheduleOfSameScheduleId_DoesNotThrowCollision()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), durasi: 60, isDraft: false);
        opCase.Schedule(existingSchedule);
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe.From(doctor));

        // The only schedule in room is the same SCH-001
        var selfScheduleView = CreateSampleScheduleOpView("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), durasi: 60);
        _scheduleOpRepoMock
            .Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", new DateTime(2026, 10, 20)))
            .Returns([selfScheduleView]);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "08:30", 60, "user-01",
            DokterOperatorId: "DOC-01",
            AlasanReschedule: "Pergeseran waktu sedikit");

        var response = await sut.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        response.ScheduleOpId.Should().Be("SCH-001");
    }

    [Fact]
    public async Task Handle_WhenRoomHasVoidSchedule_DoesNotThrowCollision()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), isDraft: false);
        opCase.Schedule(existingSchedule);
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe.From(doctor));

        // A voided schedule overlapping in room
        var voidSchedule = CreateSampleScheduleOpView("SCH-VOID", "ORD-VOID", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 10, 0, 0), durasi: 60, isVoid: true);

        _scheduleOpRepoMock
            .Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", new DateTime(2026, 10, 20)))
            .Returns([voidSchedule]);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "10:00", 60, "user-01",
            DokterOperatorId: "DOC-01",
            AlasanReschedule: "Pindah ke jam bekas void");

        var response = await sut.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        response.ScheduleOpId.Should().Be("SCH-001");
    }

    #endregion

    #region Doctor Conflict & Turnover Buffer Warnings (Soft Warning HTTP 422)

    [Fact]
    public async Task Handle_WhenDoctorHasOverlappingScheduleInAnotherRoom_AndIsOverrideFalse_ThrowsScheduleWarningException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), isDraft: false);
        opCase.Schedule(existingSchedule);
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe.From(doctor));

        // Doctor is busy in KAMAR-2 at 10:00 - 11:30
        var doctorScheduleInRoom2 = CreateSampleScheduleOpView("SCH-DOC-R2", "ORD-888", "KAMAR-2", tglOp: new DateTime(2026, 10, 20, 10, 0, 0), durasi: 90);
        _scheduleOpRepoMock
            .Setup(x => x.ListActiveSchedulesForDoctor("DOC-01", new DateTime(2026, 10, 20)))
            .Returns([doctorScheduleInRoom2]);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "10:30", 60, "user-01",
            DokterOperatorId: "DOC-01",
            AlasanReschedule: "Pindah jam operasi",
            IsOverride: false);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ScheduleWarningException>()).Which;
        ex.Warnings.Should().Contain(w => w.Contains("dr. Bedah Sp.B") && w.Contains("operasi lain"));
    }

    [Fact]
    public async Task Handle_WhenDoctorHasOverlappingScheduleInAnotherRoom_AndIsOverrideTrueWithReason_Succeeds()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), isDraft: false);
        opCase.Schedule(existingSchedule);
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe.From(doctor));

        var doctorScheduleInRoom2 = CreateSampleScheduleOpView("SCH-DOC-R2", "ORD-888", "KAMAR-2", tglOp: new DateTime(2026, 10, 20, 10, 0, 0), durasi: 90);
        _scheduleOpRepoMock
            .Setup(x => x.ListActiveSchedulesForDoctor("DOC-01", new DateTime(2026, 10, 20)))
            .Returns([doctorScheduleInRoom2]);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "10:30", 60, "user-01",
            DokterOperatorId: "DOC-01",
            AlasanReschedule: "Pindah jam operasi",
            IsOverride: true,
            OverrideReason: "Operasi pertama selesai lebih awal dan dokter siap");

        var response = await sut.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        response.ScheduleOpId.Should().Be("SCH-001");
    }

    [Fact]
    public async Task Handle_WhenDoctorHasOverlappingScheduleInAnotherRoom_AndIsOverrideTrueWithoutReason_ThrowsArgumentException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), isDraft: false);
        opCase.Schedule(existingSchedule);
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe.From(doctor));

        var doctorScheduleInRoom2 = CreateSampleScheduleOpView("SCH-DOC-R2", "ORD-888", "KAMAR-2", tglOp: new DateTime(2026, 10, 20, 10, 0, 0), durasi: 90);
        _scheduleOpRepoMock
            .Setup(x => x.ListActiveSchedulesForDoctor("DOC-01", new DateTime(2026, 10, 20)))
            .Returns([doctorScheduleInRoom2]);

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "10:30", 60, "user-01",
            DokterOperatorId: "DOC-01",
            AlasanReschedule: "Pindah jam operasi",
            IsOverride: true,
            OverrideReason: "");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Alasan diskresi override wajib diisi*");
    }

    [Fact]
    public async Task Handle_WhenTurnoverBufferViolated_AndIsOverrideFalse_ThrowsScheduleWarningException()
    {
        var sut = CreateSut();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 7, 0, 0));
        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), isDraft: false);
        opCase.Schedule(existingSchedule);
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe.From(doctor));

        // Prior schedule in KAMAR-1 ends at 10:00 (starts 09:00, durasi 60)
        var priorSchedule = CreateSampleScheduleOpView("SCH-PRIOR", "ORD-PRIOR", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 9, 0, 0), durasi: 60);
        _scheduleOpRepoMock
            .Setup(x => x.ListActiveSchedulesInRoom("KAMAR-1", new DateTime(2026, 10, 20)))
            .Returns([priorSchedule]);

        // Attempting to schedule at 10:15 (buffer is 30 mins, so buffer requirement until 10:30 is violated)
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            "ORD-001", "KAMAR-1", "2026-10-20", "10:15", 60, "user-01",
            DokterOperatorId: "DOC-01",
            TurnoverBuffer: 30,
            AlasanReschedule: "Pindah jam operasi",
            IsOverride: false);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ScheduleWarningException>()).Which;
        ex.Warnings.Should().Contain(w => w.Contains("Jeda sterilisasi (turnover buffer 30 menit) tidak terpenuhi"));
    }

    #endregion

    #region Atomic Unified Mutation & Atomic Save & Publish

    [Fact]
    public async Task Handle_UnifiedMutations_WhenAllValid_UpdatesScheduleOrderOpOpCaseAtomically()
    {
        var sut = CreateSut();
        var now = new DateTime(2026, 10, 20, 7, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), teamLeadId: "DOC-01", isDraft: false);
        opCase.Schedule(existingSchedule);

        var newDoctor = CreateSampleDoctor("DOC-02", "dr. Baru Sp.B");
        var ppaMember1 = CreateSampleDoctor("PPA-01", "dr. Anestesi Sp.An");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<PpaType>(p => p.PpaId == "DOC-02"))).Returns(MayBe.From(newDoctor));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<PpaType>(p => p.PpaId == "PPA-01"))).Returns(MayBe.From(ppaMember1));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD-001",
            KamarId: "KAMAR-1",
            Tgl: "2026-10-20",
            Jam: "10:00",
            Durasi: 90,
            UserId: "user-01",
            DokterOperatorId: "DOC-02",
            ListPpaIds: ["DOC-02", "PPA-01"],
            TurnoverBuffer: 30,
            AlasanReschedule: "Ruang operasi dialihkan jadwalnya",
            AlasanAssignLeader: "Dokter utama berhalangan hadir");

        // ACT
        var response = await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.ScheduleOpId.Should().Be("SCH-001");
        response.IsDraft.Should().BeFalse();
        response.Rescheduled.Should().BeTrue();
        response.LeaderChanged.Should().BeTrue();
        response.PpaChanged.Should().BeTrue();
        response.Published.Should().BeFalse();

        // Verify domain mutations
        existingSchedule.TeamLead.PpaId.Should().Be("DOC-02");
        existingSchedule.TglOp.Should().Be(new DateTime(2026, 10, 20, 10, 0, 0));
        existingSchedule.Durasi.Should().Be(90);
        existingSchedule.ListPpa.Should().Contain(p => p.Ppa.PpaId == "DOC-02" && p.NoUrut == 1);
        existingSchedule.ListPpa.Should().Contain(p => p.Ppa.PpaId == "PPA-01");

        // Verify two-way synchronization to OrderOp (TD-14)
        orderOp.DokterOperator.PpaId.Should().Be("DOC-02");

        // Verify atomic SaveChanges on all 3 repositories
        _scheduleOpRepoMock.Verify(x => x.SaveChanges(existingSchedule), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(opCase), Times.Once);
        _orderOpRepoMock.Verify(x => x.SaveChanges(orderOp), Times.Once);

        // Verify event publishing for official schedule reschedule
        _mediatorMock.Verify(x => x.Publish(It.IsAny<OkScheduleOpSetEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDraftAndPublishTrue_ConfirmsScheduleAndPublishesOkScheduleOpConfirmedEvent()
    {
        var sut = CreateSut();
        var now = new DateTime(2026, 10, 20, 7, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Requested);
        var existingDraftSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), isDraft: true);
        opCase.ScheduleDraft(existingDraftSchedule);
        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingDraftSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<PpaType>())).Returns(MayBe.From(doctor));

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD-001",
            KamarId: "KAMAR-1",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "user-01",
            DokterOperatorId: "DOC-01",
            Publish: true);

        // ACT
        var response = await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.Published.Should().BeTrue();
        response.IsDraft.Should().BeFalse();

        // Check aggregates confirmed
        existingDraftSchedule.IsDraft.Should().BeFalse();
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);

        // Verify domain event published
        _mediatorMock.Verify(x => x.Publish(It.Is<OkScheduleOpConfirmedEvent>(e => e.Aggregate == existingDraftSchedule), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PpaListSynchronization_RemovesOldMembersAndAddsNewMembers()
    {
        var sut = CreateSut();
        var now = new DateTime(2026, 10, 20, 7, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var doctor = CreateSampleDoctor("DOC-01", "dr. Bedah Sp.B");
        var existingNurse = CreateSampleNurse("NURSE-01", "Ns. Lama");
        var newNurse = CreateSampleNurse("NURSE-02", "Ns. Baru");

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var kamar = CreateSampleKamar("KAMAR-1");
        var opCase = CreateSampleOpCase(orderOp, OpCaseStateEnum.Scheduled);

        var initialPpaList = new List<ScheduleOpPpaType>
        {
            new(1, doctor.ToReff(), ProfesiType.Dokter, GroupSpesialisType.Default),
            new(2, existingNurse.ToReff(), ProfesiType.Perawat, GroupSpesialisType.Default)
        };
        var existingSchedule = CreateSampleScheduleOp("SCH-001", "ORD-001", "KAMAR-1", tglOp: new DateTime(2026, 10, 20, 8, 0, 0), teamLeadId: "DOC-01", isDraft: false, listPpa: initialPpaList);
        opCase.Schedule(existingSchedule);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(orderOp));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<KamarType>())).Returns(MayBe.From(kamar));
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<OrderOpModel>())).Returns(MayBe.From(opCase));
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<ScheduleOpModel>())).Returns(MayBe.From(existingSchedule));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<PpaType>(p => p.PpaId == "DOC-01"))).Returns(MayBe.From(doctor));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<PpaType>(p => p.PpaId == "NURSE-01"))).Returns(MayBe.From(existingNurse));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<PpaType>(p => p.PpaId == "NURSE-02"))).Returns(MayBe.From(newNurse));

        // Time and leader unchanged, but PPA list replaces NURSE-01 with NURSE-02
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD-001",
            KamarId: "KAMAR-1",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "user-01",
            DokterOperatorId: "DOC-01",
            ListPpaIds: ["DOC-01", "NURSE-02"]);

        // ACT
        var response = await sut.Handle(cmd, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.PpaChanged.Should().BeTrue();
        response.Rescheduled.Should().BeFalse();
        response.LeaderChanged.Should().BeFalse();

        existingSchedule.ListPpa.Should().NotContain(p => p.Ppa.PpaId == "NURSE-01");
        existingSchedule.ListPpa.Should().Contain(p => p.Ppa.PpaId == "NURSE-02");
        existingSchedule.ListPpa.Should().Contain(p => p.Ppa.PpaId == "DOC-01" && p.NoUrut == 1);
    }

    #endregion
}
