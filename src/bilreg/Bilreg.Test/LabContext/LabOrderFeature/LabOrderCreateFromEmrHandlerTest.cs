using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderCreateFromEmrHandlerTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly Mock<ISequencer> _sequencer = new();
    private readonly Mock<ILabTestResolutionService> _resolution = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IPpaRepo> _ppaRepo = new();
    private readonly Mock<ILabOrderDal> _labOrderDal = new();
    private readonly LabOrderCreateFromEmrHandler _sut;

    public LabOrderCreateFromEmrHandlerTest()
    {
        _sequencer.Setup(s => s.GetNextNoUrut("LAB")).Returns(42);
        _resolution
            .Setup(r => r.ResolveByTarifItems(It.IsAny<IEnumerable<LabOrderTarifItemInput>>()))
            .Returns([new ResolvedLabOrderLine(LabOrderTestSupport.SampleItem(), [])]);

        // Default: active registration
        _regRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(CreateTestReg("REG001")));

        // Default: valid doctor
        var doctor = PpaType.Default with { PpaId = "DR01", PpaName = "dr. Strange" };
        _ppaRepo.Setup(r => r.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(doctor));

        // Default: no duplicate active tests
        _labOrderDal.Setup(d => d.ListActiveTestsByPatientWithinHours(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>()))
            .Returns([]);

        _sut = new LabOrderCreateFromEmrHandler(
            _repo.Object,
            _sequencer.Object,
            _resolution.Object,
            TestTglJamProvider.Instance,
            _regRepo.Object,
            _ppaRepo.Object,
            _labOrderDal.Object);
    }

    private static RegModel CreateTestReg(string regId = "REG001", bool isDischarged = false, bool isVoided = false)
    {
        return new RegModel(
            regId,
            new DateOnly(2026, 1, 1),
            new AuditInfoType("tester", DateTime.Now),
            isDischarged ? new AuditInfoType("discharger", DateTime.Now) : AuditInfoType.Default,
            AuditInfoType.Default,
            isVoided ? new AuditInfoType("voider", DateTime.Now) : AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);
    }

    private static LabOrderCreateFromEmrCmd ValidCmd() =>
        new(
            UserId: "U1",
            EmrOrderId: "EMR-ORDER-001",
            RegId: "REG001",
            PatientId: "MR0001",
            PatientName: "Pasien Tes",
            BirthDateYmd: "1990-05-15",
            Gender: "L",
            Items: [new LabOrderTarifItemInput("TR1", "Tarif HB")],
            DokterPerujukId: "DR01",
            DokterPerujukName: "dr. Strange",
            LayananPerujukId: "LAY01",
            LayananPerujukName: "Poli Penyakit Dalam",
            ClinicalNote: "Demam berdarah tersangka",
            Urgency: 1,
            AllowDuplicateConfirmation: false);

    [Fact]
    public async Task Handle_ValidRequest_SavesAndReturnsEmrAck_WithClinicalContextAndCito()
    {
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(ValidCmd(), CancellationToken.None);

        result.EmrOrderId.Should().Be("EMR-ORDER-001");
        result.OrderNo.Should().Be("LAB00000042");
        result.LabOrderStatus.Should().Be((int)LabOrderStatusEnum.Ordered);
        result.OrderId.Should().NotBeNullOrWhiteSpace();
        persisted.Should().NotBeNull();
        result.OrderId.Should().Be(persisted!.OrderId);
        persisted!.EmrOrderId.Should().Be("EMR-ORDER-001");
        persisted.DokterPerujukId.Should().Be("DR01");
        persisted.DokterPerujukName.Should().Be("dr. Strange");
        persisted.LayananPerujukId.Should().Be("LAY01");
        persisted.LayananPerujukName.Should().Be("Poli Penyakit Dalam");
        persisted.ClinicalNote.Should().Be("Demam berdarah tersangka");
        persisted.Urgency.Should().Be(LabOrderUrgencyEnum.Cito);
        persisted.IsCito.Should().BeTrue();
        persisted.Items.Should().HaveCount(1);
        persisted.Items.First().TestDefinitionId.Should().Be("LTD0001");
        persisted.Items.First().Urgency.Should().Be(LabOrderUrgencyEnum.Cito);
        persisted.Items.First().IsCito.Should().BeTrue();

        _resolution.Verify(r => r.ResolveByTarifItems(It.IsAny<IEnumerable<LabOrderTarifItemInput>>()), Times.Once);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RegNotFound_ThrowsEX01()
    {
        _regRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var act = async () => await _sut.Handle(ValidCmd(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*EX-01*");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegDischarged_ThrowsEX01()
    {
        var dischargedReg = CreateTestReg("REG001", isDischarged: true);
        _regRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(dischargedReg));

        var act = async () => await _sut.Handle(ValidCmd(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*EX-01*");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegVoided_ThrowsEX01()
    {
        var voidedReg = CreateTestReg("REG001", isVoided: true);
        _regRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(voidedReg));

        var act = async () => await _sut.Handle(ValidCmd(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*EX-01*");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DokterPerujukNotFound_ThrowsException()
    {
        _ppaRepo.Setup(r => r.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe<PpaType>.None);

        var cmd = ValidCmd() with { DokterPerujukId = "DR-UNKNOWN" };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*tidak ditemukan*");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DokterPerujukEmpty_BypassesPpaRepoCheck()
    {
        var cmd = ValidCmd() with { DokterPerujukId = "", DokterPerujukName = "" };

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        _ppaRepo.Verify(r => r.LoadEntity(It.IsAny<IPpaKey>()), Times.Never);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateTestWithin24Hours_WithoutConfirmation_ThrowsEX05()
    {
        _labOrderDal.Setup(d => d.ListActiveTestsByPatientWithinHours("MR0001", 24, It.IsAny<DateTime>()))
            .Returns([
                new LabOrderActiveTestDto(
                    "ORD-PREV-01",
                    "LAB00000001",
                    "LTD0001",
                    "HB",
                    "Hemoglobin",
                    DateTime.Now.AddHours(-3),
                    (int)LabOrderStatusEnum.Ordered)
            ]);

        var cmd = ValidCmd() with { AllowDuplicateConfirmation = false };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DuplicateLabOrderWarningException>();
        ex.WithMessage("*EX-05*");
        ex.Which.DuplicateTestNames.Should().Contain("Hemoglobin");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateTestWithin24Hours_WithConfirmation_SavesSuccessfully()
    {
        _labOrderDal.Setup(d => d.ListActiveTestsByPatientWithinHours("MR0001", 24, It.IsAny<DateTime>()))
            .Returns([
                new LabOrderActiveTestDto(
                    "ORD-PREV-01",
                    "LAB00000001",
                    "LTD0001",
                    "HB",
                    "Hemoglobin",
                    DateTime.Now.AddHours(-3),
                    (int)LabOrderStatusEnum.Ordered)
            ]);

        var cmd = ValidCmd() with { AllowDuplicateConfirmation = true };
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyRegId_ThrowsArgumentException()
    {
        var cmd = ValidCmd() with { RegId = "" };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyItems_ThrowsArgumentException()
    {
        var cmd = ValidCmd() with { Items = [] };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WithRegPasienId_NormalizesPatientIdInSnapshotAndDalCall()
    {
        var regWithPasien = new RegModel(
            "REG001",
            new DateOnly(2026, 1, 1),
            new AuditInfoType("tester", DateTime.Now),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            new PasienReff("PASIEN-REG-01", "Pasien Dari Reg", new DateOnly(1990, 1, 1), "L"),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);
        _regRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(regWithPasien));

        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var cmd = ValidCmd() with { PatientId = "FALLBACK-MR" };
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        persisted.Should().NotBeNull();
        persisted!.Patient.PatientId.Should().Be("PASIEN-REG-01");
        _labOrderDal.Verify(d => d.ListActiveTestsByPatientWithinHours("PASIEN-REG-01", 24, It.IsAny<DateTime>()), Times.Once);
    }
}
