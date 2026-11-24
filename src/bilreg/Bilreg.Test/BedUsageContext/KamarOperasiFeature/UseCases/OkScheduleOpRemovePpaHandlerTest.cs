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

public class OkScheduleOpRemovePpaHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private OkScheduleOpRemovePpaHandler CreateSut()
    {
        return new OkScheduleOpRemovePpaHandler(
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

    private static PpaType CreateSamplePpa(string ppaId = "DOC-02", string ppaName = "dr. Anestesi Sp.An", ProfesiType? profesi = null)
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

    private static ScheduleOpModel CreateSampleScheduleOpWithTwoPpa(
        string scheduleOpId = "SCH-001",
        string orderOpId = "ORD-001",
        string leadDoctorId = "DOC-01",
        string secondDoctorId = "DOC-02",
        bool isDraft = false)
    {
        var opTime = new DateTime(2026, 10, 20, 8, 0, 0);
        var ppa1 = new ScheduleOpPpaType(1, new PpaReff(leadDoctorId, "dr. Bedah Sp.B"), ProfesiType.Dokter, GroupSpesialisType.Default);
        var ppa2 = new ScheduleOpPpaType(2, new PpaReff(secondDoctorId, "dr. Anestesi Sp.An"), ProfesiType.Dokter, GroupSpesialisType.Default);
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
            isDraft ? OpCaseStateEnum.Requested : OpCaseStateEnum.Scheduled,
            [ppa1, ppa2],
            "",
            30,
            [],
            isDraft: isDraft);
    }

    [Fact]
    public async Task Handle_RemovePpa_OnDraftSchedule_PreservesRequestedState()
    {
        // Arrange
        var now = new DateTime(2026, 10, 20, 7, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var ppaToRemove = CreateSamplePpa("DOC-02", "dr. Anestesi Sp.An");
        var opCase = OpCaseModel.Create(orderOp, now);
        var draftSchedule = CreateSampleScheduleOpWithTwoPpa("SCH-001", "ORD-001", "DOC-01", "DOC-02", isDraft: true);
        opCase.ScheduleDraft(draftSchedule, now);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-02")))
            .Returns(MayBe.From(ppaToRemove));
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
            OpCaseStateEnum.Requested,
            false,
            IsDraft: true);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(draftSchedule));

        var cmd = new OkScheduleOpRemovePpaCommand("ORD-001", "DOC-02", "user-01");
        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Requested);
        draftSchedule.ListPpa.Should().NotContain(x => x.Ppa.PpaId == "DOC-02");
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.Is<OpCaseModel>(c => c.OrderOpState == OpCaseStateEnum.Requested)), Times.Once);
        _scheduleOpRepoMock.Verify(x => x.SaveChanges(draftSchedule), Times.Once);
    }

    [Fact]
    public async Task Handle_RemovePpa_OnConfirmedSchedule_MaintainsScheduledState()
    {
        // Arrange
        var now = new DateTime(2026, 10, 20, 7, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var ppaToRemove = CreateSamplePpa("DOC-02", "dr. Anestesi Sp.An");
        var opCase = OpCaseModel.Create(orderOp, now);
        var confirmedSchedule = CreateSampleScheduleOpWithTwoPpa("SCH-001", "ORD-001", "DOC-01", "DOC-02", isDraft: false);
        opCase.Schedule(confirmedSchedule, now);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-02")))
            .Returns(MayBe.From(ppaToRemove));
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
            false,
            IsDraft: false);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(confirmedSchedule));

        var cmd = new OkScheduleOpRemovePpaCommand("ORD-001", "DOC-02", "user-01");
        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);
        confirmedSchedule.ListPpa.Should().NotContain(x => x.Ppa.PpaId == "DOC-02");
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.Is<OpCaseModel>(c => c.OrderOpState == OpCaseStateEnum.Scheduled)), Times.Once);
        _scheduleOpRepoMock.Verify(x => x.SaveChanges(confirmedSchedule), Times.Once);
    }

    [Fact]
    public async Task Handle_RemovePpa_WhenPpaIsLeadDoctor_ThrowsArgumentException()
    {
        // Arrange
        var now = new DateTime(2026, 10, 20, 7, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var orderOp = CreateSampleOrderOp("ORD-001", "DOC-01");
        var leadDoctor = CreateSamplePpa("DOC-01", "dr. Bedah Sp.B");
        var opCase = OpCaseModel.Create(orderOp, now);
        var draftSchedule = CreateSampleScheduleOpWithTwoPpa("SCH-001", "ORD-001", "DOC-01", "DOC-02", isDraft: true);
        opCase.ScheduleDraft(draftSchedule, now);

        _orderOpRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(leadDoctor));
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
            OpCaseStateEnum.Requested,
            false,
            IsDraft: true);

        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns(new List<ScheduleOpView> { scheduleView });
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.Is<IScheduleOpKey>(k => k.ScheduleOpId == "SCH-001")))
            .Returns(MayBe.From(draftSchedule));

        var cmd = new OkScheduleOpRemovePpaCommand("ORD-001", "DOC-01", "user-01");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Dokter Operator Utama aktif tidak dapat dihapus*");
    }
}
