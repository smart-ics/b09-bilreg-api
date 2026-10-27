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
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkScheduleOpGetQueryTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();

    private OkScheduleOpGetHandler CreateSut()
    {
        return new OkScheduleOpGetHandler(_scheduleOpRepoMock.Object, _orderOpRepoMock.Object);
    }

    private static OrderOpModel CreateSampleOrderOp(string orderOpId = "ORD-001", string pasienId = "PAS-001")
    {
        return new OrderOpModel(
            orderOpId,
            new DateTime(2026, 10, 16),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 16)),
            new PasienReff(pasienId, "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new RegReff("REG-001", pasienId, "Pasien Test"),
            new Icd10Type("K40", "Hernia"),
            new JenisOperasiType("JOP-1", "Bedah"),
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
            AnestesiPlanEnum.RegionalSpinal,
            PostOpCareUnitEnum.PACU,
            "",
            []);
    }

    private static ScheduleOpModel CreateSampleScheduleOp(
        string scheduleOpId = "SCH-001",
        string orderOpId = "ORD-001",
        string pasienId = "PAS-001",
        OpCaseStateEnum state = OpCaseStateEnum.Scheduled)
    {
        var hist = new ScheduleOpHistType(
            1,
            new DateTime(2026, 10, 20, 8, 0, 0),
            new DateTime(2026, 10, 20, 10, 0, 0),
            60,
            90,
            new KamarReff("KAMAR-1", "OK 1"),
            new KamarReff("KAMAR-2", "OK 2"),
            new PpaReff("DOC-01", "dr. Bedah"),
            new PpaReff("DOC-02", "dr. Baru"),
            "Pergeseran waktu",
            true,
            "Disetujui Ka. OK",
            "user1",
            new DateTime(2026, 10, 19, 14, 0, 0));

        var ppaMember = new ScheduleOpPpaType(1, new PpaReff("DOC-02", "dr. Baru"), ProfesiType.Dokter, GroupSpesialisType.Default);

        return new ScheduleOpModel(
            scheduleOpId,
            new DateTime(2026, 10, 16),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 16)),
            new OrderOpReff(orderOpId, new DateTime(2026, 10, 16), "Herniotomi"),
            new PasienReff(pasienId, "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            UrgencyLevelEnum.Elective,
            90,
            new DateTime(2026, 10, 20, 10, 0, 0),
            new KamarReff("KAMAR-2", "OK 2"),
            new RegReff("REG-001", pasienId, "Pasien Test"),
            new PpaReff("DOC-02", "dr. Baru"),
            new DateTime(2026, 10, 20, 10, 0, 0),
            new DateTime(2026, 10, 20, 11, 30, 0),
            state,
            [ppaMember],
            "Alasan batal uji",
            45,
            [hist]);
    }

    [Fact]
    public async Task Handle_WhenOrderOpNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe<OrderOpModel>.None);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(new OkScheduleOpGetQuery("ORD-404"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_WhenScheduleNotFoundForOrderOp_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001", "PAS-001");
        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == "PAS-001")))
            .Returns(new List<ScheduleOpView>());

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(new OkScheduleOpGetQuery("ORD-001"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_WhenScheduleIsVoid_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001", "PAS-001");
        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));

        var voidScheduleView = new ScheduleOpView(
            "SCH-001",
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new OrderOpReff("ORD-001", new DateTime(2026, 10, 16), "Herniotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 0, 0),
            60,
            new PpaReff("DOC-01", "dr. Bedah"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            true,
            45);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == "PAS-001")))
            .Returns(new List<ScheduleOpView> { voidScheduleView });

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(new OkScheduleOpGetQuery("ORD-001"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_WhenScheduleFound_ShouldMapAllFieldsIncludingAlasanBatalTurnoverBufferAndHistory()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001", "PAS-001");
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", "PAS-001", OpCaseStateEnum.Scheduled);

        var scheduleView = new ScheduleOpView(
            "SCH-001",
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new OrderOpReff("ORD-001", new DateTime(2026, 10, 16), "Herniotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 10, 0, 0),
            90,
            new PpaReff("DOC-02", "dr. Baru"),
            new KamarReff("KAMAR-2", "OK 2"),
            OpCaseStateEnum.Scheduled,
            false,
            45);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == "PAS-001")))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(scheduleOp));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new OkScheduleOpGetQuery("ORD-001"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderOpId.Should().Be("ORD-001");
        result.ScheduleOpId.Should().Be("SCH-001");
        result.NamaOperasi.Should().Be("Herniotomi");
        result.Urgency.Should().Be("Elective");
        result.Durasi.Should().Be(90);
        result.PasienId.Should().Be("PAS-001");
        result.PasienName.Should().Be("Pasien Test");
        result.RegId.Should().Be("REG-001");
        result.TglOp.Should().Be("2026-10-20");
        result.JamOp.Should().Be("10:00");
        result.KamarId.Should().Be("KAMAR-2");
        result.KamarName.Should().Be("OK 2");
        result.DokterId.Should().Be("DOC-02");
        result.DokterName.Should().Be("dr. Baru");
        result.StatusOp.Should().Be("Scheduled");
        result.AlasanBatal.Should().Be("Alasan batal uji");
        result.TurnoverBuffer.Should().Be(45);
        result.ListPpa.Should().HaveCount(1);
        var ppa = result.ListPpa.First();
        ppa.NoUrut.Should().Be(1);
        ppa.PpaId.Should().Be("DOC-02");
        ppa.PpaName.Should().Be("dr. Baru");
        ppa.ProfesiId.Should().Be("DOK");
        ppa.ProfesiName.Should().Be("Dokter");

        result.History.Should().HaveCount(1);
        var hist = result.History.First();
        hist.NoUrut.Should().Be(1);
        hist.OldTglOp.Should().Be(new DateTime(2026, 10, 20, 8, 0, 0));
        hist.NewTglOp.Should().Be(new DateTime(2026, 10, 20, 10, 0, 0));
        hist.OldDurasi.Should().Be(60);
        hist.NewDurasi.Should().Be(90);
        hist.OldKamarId.Should().Be("KAMAR-1");
        hist.OldKamarName.Should().Be("OK 1");
        hist.NewKamarId.Should().Be("KAMAR-2");
        hist.NewKamarName.Should().Be("OK 2");
        hist.OldTeamLeadId.Should().Be("DOC-01");
        hist.OldTeamLeadName.Should().Be("dr. Bedah");
        hist.NewTeamLeadId.Should().Be("DOC-02");
        hist.NewTeamLeadName.Should().Be("dr. Baru");
        hist.Alasan.Should().Be("Pergeseran waktu");
        hist.IsOverride.Should().BeTrue();
        hist.OverrideReason.Should().Be("Disetujui Ka. OK");
        hist.UserId.Should().Be("user1");
        hist.CrtDate.Should().Be(new DateTime(2026, 10, 19, 14, 0, 0));
        hist.ChangeType.Should().Be("RESCHEDULE");
    }

    [Fact]
    public async Task Handle_WhenScheduleHasMultiplePpaAndAssignLeaderHistory_ShouldMapPpaDetailsAndChangeTypeCorrectly()
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-002", "PAS-002");
        var hist = new ScheduleOpHistType(
            1,
            new DateTime(2026, 10, 21, 8, 0, 0),
            new DateTime(2026, 10, 21, 8, 0, 0),
            60,
            60,
            new KamarReff("KAMAR-1", "OK 1"),
            new KamarReff("KAMAR-1", "OK 1"),
            new PpaReff("DOC-01", "dr. Lama"),
            new PpaReff("DOC-02", "dr. Baru"),
            "Pergantian Operator",
            false,
            "",
            "user2",
            new DateTime(2026, 10, 20, 9, 0, 0),
            "ASSIGN_LEADER");

        var ppaDoctor = new ScheduleOpPpaType(1, new PpaReff("DOC-02", "dr. Baru"), ProfesiType.Dokter, GroupSpesialisType.Default);
        var ppaNurse = new ScheduleOpPpaType(2, new PpaReff("PRW-01", "Ns. Asisten"), ProfesiType.Perawat, GroupSpesialisType.Default);

        var scheduleOp = new ScheduleOpModel(
            "SCH-002",
            new DateTime(2026, 10, 16),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 16)),
            new OrderOpReff("ORD-002", new DateTime(2026, 10, 16), "Apendektomi"),
            new PasienReff("PAS-002", "Pasien Dua", new DateOnly(1995, 5, 5), "P"),
            UrgencyLevelEnum.Cito,
            60,
            new DateTime(2026, 10, 21, 8, 0, 0),
            new KamarReff("KAMAR-1", "OK 1"),
            new RegReff("REG-002", "PAS-002", "Pasien Dua"),
            new PpaReff("DOC-02", "dr. Baru"),
            new DateTime(2026, 10, 21, 8, 0, 0),
            new DateTime(2026, 10, 21, 9, 0, 0),
            OpCaseStateEnum.Scheduled,
            [ppaDoctor, ppaNurse],
            "",
            30,
            [hist]);

        var scheduleView = new ScheduleOpView(
            "SCH-002",
            new PasienReff("PAS-002", "Pasien Dua", new DateOnly(1995, 5, 5), "P"),
            new OrderOpReff("ORD-002", new DateTime(2026, 10, 16), "Apendektomi"),
            UrgencyLevelEnum.Cito,
            new DateTime(2026, 10, 21, 8, 0, 0),
            60,
            new PpaReff("DOC-02", "dr. Baru"),
            new KamarReff("KAMAR-1", "OK 1"),
            OpCaseStateEnum.Scheduled,
            false,
            30);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-002")))
            .Returns(MayBe.From(orderOp));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.Is<IPasienKey>(k => k.PasienId == "PAS-002")))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-002")))
            .Returns(MayBe.From(scheduleOp));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new OkScheduleOpGetQuery("ORD-002"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ListPpa.Should().HaveCount(2);

        var listPpa = result.ListPpa.ToList();
        listPpa[0].NoUrut.Should().Be(1);
        listPpa[0].PpaId.Should().Be("DOC-02");
        listPpa[0].PpaName.Should().Be("dr. Baru");
        listPpa[0].ProfesiId.Should().Be("DOK");
        listPpa[0].ProfesiName.Should().Be("Dokter");

        listPpa[1].NoUrut.Should().Be(2);
        listPpa[1].PpaId.Should().Be("PRW-01");
        listPpa[1].PpaName.Should().Be("Ns. Asisten");
        listPpa[1].ProfesiId.Should().Be("PRW");
        listPpa[1].ProfesiName.Should().Be("Perawat");

        result.History.Should().HaveCount(1);
        result.History.First().ChangeType.Should().Be("ASSIGN_LEADER");
    }

    [Theory]
    [InlineData(OpCaseStateEnum.Scheduled, "Scheduled")]
    [InlineData(OpCaseStateEnum.OpStarted, "In-Progress")]
    [InlineData(OpCaseStateEnum.RecoveryStarted, "In-Recovery")]
    [InlineData(OpCaseStateEnum.Requested, "Others")]
    [InlineData(OpCaseStateEnum.Discharged, "Others")]
    public async Task Handle_ShouldTranslateOpCaseStateAccurately(OpCaseStateEnum state, string expectedStatus)
    {
        // Arrange
        var orderOp = CreateSampleOrderOp("ORD-001", "PAS-001");
        var scheduleOp = CreateSampleScheduleOp("SCH-001", "ORD-001", "PAS-001", state);
        var scheduleView = new ScheduleOpView(
            "SCH-001",
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new OrderOpReff("ORD-001", new DateTime(2026, 10, 16), "Herniotomi"),
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 10, 0, 0),
            90,
            new PpaReff("DOC-02", "dr. Baru"),
            new KamarReff("KAMAR-2", "OK 2"),
            state,
            false,
            45);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns(MayBe.From(orderOp));
        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>()))
            .Returns(MayBe.From(scheduleOp));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new OkScheduleOpGetQuery("ORD-001"), CancellationToken.None);

        // Assert
        result.StatusOp.Should().Be(expectedStatus);
    }
}
