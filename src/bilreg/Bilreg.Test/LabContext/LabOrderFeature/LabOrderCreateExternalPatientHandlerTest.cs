using Bilreg.Application.LabContext.LabExternalFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderCreateExternalPatientHandlerTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly Mock<ISequencer> _sequencer = new();
    private readonly Mock<ILabTestResolutionService> _resolution = new();
    private readonly Mock<ILabExternalRepo> _labExternalRepo = new();
    private readonly Mock<ILabOrderDal> _labOrderDal = new();
    private readonly LabOrderCreateExternalPatientHandler _sut;

    public LabOrderCreateExternalPatientHandlerTest()
    {
        _sequencer.Setup(s => s.GetNextNoUrut("LAB")).Returns(55);
        _resolution
            .Setup(r => r.ResolveByTarifItems(It.IsAny<IEnumerable<LabOrderTarifItemInput>>()))
            .Returns([new ResolvedLabOrderLine(LabOrderTestSupport.SampleItem(), [])]);

        // Default: active external registration
        _labExternalRepo.Setup(r => r.LoadEntity(It.IsAny<ILabExternalKey>()))
            .Returns(MayBe.From(CreateActiveExternal()));

        // Default: no duplicate active tests
        _labOrderDal.Setup(d => d.ListActiveTestsByPatientWithinHours(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>()))
            .Returns([]);

        _sut = new LabOrderCreateExternalPatientHandler(
            _repo.Object,
            _sequencer.Object,
            _resolution.Object,
            TestTglJamProvider.Instance,
            _labExternalRepo.Object,
            _labOrderDal.Object);
    }

    private static LabExternalModel CreateActiveExternal(
        string regExternalId = "LEX26A14B8C1D",
        string externalMrNo = "EXT-202610-00001",
        string doctor = "dr. Luar",
        string origin = "Klinik Pratama",
        string notes = "Cek rutin",
        LabExternalStatusEnum status = LabExternalStatusEnum.Active)
    {
        var demographics = new LabExternalDemographicsType(
            "Budi Pasien Luar",
            "3171012345670001",
            new DateTime(1988, 12, 5),
            "L",
            "08123456789",
            "Jl. Sudirman No. 1");

        var referralInfo = new LabExternalReferralInfoType(
            LabReferralTypeEnum.FaskesLuar,
            doctor,
            origin,
            notes);

        var auditTrail = AuditTrailType.Create("U1", DateTime.Now);

        return LabExternalModel.Load(
            regExternalId,
            externalMrNo,
            demographics,
            referralInfo,
            regExternalId,
            status,
            LabExternalCancelInfoType.Default,
            auditTrail);
    }

    private static LabOrderCreateExternalPatientCmd ValidCmdWithRegExternalId() =>
        new(
            UserId: "USER-01",
            EmrOrderId: null,
            RegId: null,
            PatientId: null,
            PatientName: "Budi Pasien Luar",
            BirthDateYmd: "1988-12-05",
            Gender: "L",
            Items: [new LabOrderTarifItemInput("TR1", "Tarif Hemoglobin")],
            RegExternalId: "LEX26A14B8C1D",
            ExternalMrNo: "EXT-202610-00001",
            Urgency: 1);

    private static LabOrderCreateExternalPatientCmd ValidCmdWithRegId() =>
        new(
            UserId: "USER-01",
            EmrOrderId: null,
            RegId: "LEX26A14B8C1D",
            PatientId: "EXT-202610-00001",
            PatientName: "Budi Pasien Luar",
            BirthDateYmd: "1988-12-05",
            Gender: "L",
            Items: [new LabOrderTarifItemInput("TR1", "Tarif Hemoglobin")]);

    [Fact]
    public async Task Handle_WithRegExternalId_AutoPopulatesFromReferralInfo_SavesOrder()
    {
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(ValidCmdWithRegExternalId(), CancellationToken.None);

        result.OrderId.Should().NotBeNullOrWhiteSpace();
        result.OrderNo.Should().Be("LAB00000055");
        persisted.Should().NotBeNull();
        persisted!.OrderSource.Should().Be(LabOrderSourceEnum.ExternalPatient);
        persisted.OrderNo.Should().Be("LAB00000055");
        persisted.Patient.RegId.Should().Be("LEX26A14B8C1D");
        persisted.Patient.PatientId.Should().Be("EXT-202610-00001");
        persisted.Patient.PatientName.Should().Be("Budi Pasien Luar");
        persisted.DokterPerujukName.Should().Be("dr. Luar");
        persisted.LayananPerujukName.Should().Be("Klinik Pratama");
        persisted.ClinicalNote.Should().Be("Cek rutin");
        persisted.Urgency.Should().Be(LabOrderUrgencyEnum.Cito);
        persisted.IsCito.Should().BeTrue();
        persisted.Items.Should().HaveCount(1);
        persisted.Items.First().Urgency.Should().Be(LabOrderUrgencyEnum.Cito);

        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyReferralInfo_DefaultsToAps()
    {
        var externalModel = CreateActiveExternal(doctor: "", origin: "", notes: "");
        _labExternalRepo.Setup(r => r.LoadEntity(It.IsAny<ILabExternalKey>()))
            .Returns(MayBe.From(externalModel));

        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var cmd = ValidCmdWithRegExternalId() with
        {
            DokterPerujukName = "",
            LayananPerujukName = "",
            ClinicalNote = ""
        };

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        persisted.Should().NotBeNull();
        persisted!.DokterPerujukName.Should().Be("APS");
        persisted.LayananPerujukName.Should().Be("APS");
        persisted.ClinicalNote.Should().Be("APS");
    }

    [Fact]
    public async Task Handle_ExplicitPerujukOverridesAutoPopulation()
    {
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var cmd = ValidCmdWithRegExternalId() with
        {
            DokterPerujukName = "dr. Spesifik",
            LayananPerujukName = "RS Luar Kota",
            ClinicalNote = "Catatan Khusus Dokter"
        };

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        persisted.Should().NotBeNull();
        persisted!.DokterPerujukName.Should().Be("dr. Spesifik");
        persisted.LayananPerujukName.Should().Be("RS Luar Kota");
        persisted.ClinicalNote.Should().Be("Catatan Khusus Dokter");
    }

    [Fact]
    public async Task Handle_WithRegIdAndPatientId_MapsToSnapshotAndSavesOrder()
    {
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(ValidCmdWithRegId(), CancellationToken.None);

        result.OrderId.Should().NotBeNullOrWhiteSpace();
        result.OrderNo.Should().Be("LAB00000055");
        persisted.Should().NotBeNull();
        persisted!.OrderSource.Should().Be(LabOrderSourceEnum.ExternalPatient);
        persisted.Patient.RegId.Should().Be("LEX26A14B8C1D");
        persisted.Patient.PatientId.Should().Be("EXT-202610-00001");
        persisted.Patient.PatientName.Should().Be("Budi Pasien Luar");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExternalRegNotFound_ThrowsException()
    {
        _labExternalRepo.Setup(r => r.LoadEntity(It.IsAny<ILabExternalKey>()))
            .Returns(MayBe<LabExternalModel>.None);

        var act = async () => await _sut.Handle(ValidCmdWithRegExternalId(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*tidak ditemukan*");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExternalRegInactive_ThrowsException()
    {
        var inactiveExternal = CreateActiveExternal(status: LabExternalStatusEnum.Cancelled);
        _labExternalRepo.Setup(r => r.LoadEntity(It.IsAny<ILabExternalKey>()))
            .Returns(MayBe.From(inactiveExternal));

        var act = async () => await _sut.Handle(ValidCmdWithRegExternalId(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*tidak aktif*");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateTestWithin24Hours_WithoutConfirmation_ThrowsEX05()
    {
        _labOrderDal.Setup(d => d.ListActiveTestsByPatientWithinHours("EXT-202610-00001", 24, It.IsAny<DateTime>()))
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

        var cmd = ValidCmdWithRegExternalId() with { AllowDuplicateConfirmation = false };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DuplicateLabOrderWarningException>();
        ex.WithMessage("*EX-05*");
        ex.Which.DuplicateTestNames.Should().Contain("Hemoglobin");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateTestWithin24Hours_WithConfirmation_SavesSuccessfully()
    {
        _labOrderDal.Setup(d => d.ListActiveTestsByPatientWithinHours("EXT-202610-00001", 24, It.IsAny<DateTime>()))
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

        var cmd = ValidCmdWithRegExternalId() with { AllowDuplicateConfirmation = true };
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyUserId_ThrowsArgumentException()
    {
        var cmd = ValidCmdWithRegExternalId() with { UserId = "" };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyPatientName_ThrowsArgumentException()
    {
        var cmd = ValidCmdWithRegExternalId() with { PatientName = "" };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyItems_ThrowsArgumentException()
    {
        var cmd = ValidCmdWithRegExternalId() with { Items = [] };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_With12CharRegExternalIdAnd16CharExternalMrNo_MapsToSnapshotAndSavesOrder()
    {
        // AC-ARCH-09 & TD-09: Ensure order creation handles EffectiveRegId (12 chars) and EffectivePatientId (16 chars, e.g. EXT-202610-00001)
        var reg12Char = "LEX26A150001";
        var mr16Char = "EXT-202610-00001";
        reg12Char.Length.Should().Be(12);
        mr16Char.Length.Should().Be(16);

        var externalModel = CreateActiveExternal(regExternalId: reg12Char, externalMrNo: mr16Char);
        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == reg12Char)))
            .Returns(MayBe.From(externalModel));

        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var cmd = new LabOrderCreateExternalPatientCmd(
            UserId: "USER-01",
            EmrOrderId: null,
            RegId: null,
            PatientId: null,
            PatientName: "Pasien Uji Sizing 12-16",
            BirthDateYmd: "1990-01-01",
            Gender: "L",
            Items: [new LabOrderTarifItemInput("TR1", "Tarif Hemoglobin")],
            RegExternalId: reg12Char,
            ExternalMrNo: mr16Char,
            Urgency: 0);

        cmd.EffectiveRegId.Should().Be(reg12Char);
        cmd.EffectiveRegId.Length.Should().Be(12);
        cmd.EffectivePatientId.Should().Be(mr16Char);
        cmd.EffectivePatientId.Length.Should().Be(16);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        persisted.Should().NotBeNull();
        persisted!.Patient.RegId.Should().Be(reg12Char);
        persisted.Patient.RegId.Length.Should().Be(12);
        persisted.Patient.PatientId.Should().Be(mr16Char);
        persisted.Patient.PatientId.Length.Should().Be(16);
        persisted.OrderSource.Should().Be(LabOrderSourceEnum.ExternalPatient);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFallbackRegIdAndPatientId_12CharAnd16Char_MapsCorrectly()
    {
        // AC-ARCH-09 & TD-09: Ensure order creation with fallback RegId (12 chars) and PatientId (16 chars)
        var reg12Char = "LEX26A150002";
        var mr16Char = "EXT-202610-00002";
        reg12Char.Length.Should().Be(12);
        mr16Char.Length.Should().Be(16);

        var externalModel = CreateActiveExternal(regExternalId: reg12Char, externalMrNo: mr16Char);
        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == reg12Char)))
            .Returns(MayBe.From(externalModel));

        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var cmd = new LabOrderCreateExternalPatientCmd(
            UserId: "USER-01",
            EmrOrderId: null,
            RegId: reg12Char,
            PatientId: mr16Char,
            PatientName: "Pasien Uji Fallback Sizing",
            BirthDateYmd: "1990-01-01",
            Gender: "L",
            Items: [new LabOrderTarifItemInput("TR1", "Tarif Hemoglobin")]);

        cmd.EffectiveRegId.Should().Be(reg12Char);
        cmd.EffectiveRegId.Length.Should().Be(12);
        cmd.EffectivePatientId.Should().Be(mr16Char);
        cmd.EffectivePatientId.Length.Should().Be(16);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        persisted.Should().NotBeNull();
        persisted!.Patient.RegId.Should().Be(reg12Char);
        persisted.Patient.RegId.Length.Should().Be(12);
        persisted.Patient.PatientId.Should().Be(mr16Char);
        persisted.Patient.PatientId.Length.Should().Be(16);
    }
}
