using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
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
using Bilreg.Infrastructure.LabContext;
using Bilreg.Infrastructure.LabContext.Integration;
using Bilreg.Test.Shared;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabRegIntegrationTest
{
    private readonly Mock<ILabOrderRepo> _labOrderRepo = new();
    private readonly Mock<IRegAktifRepo> _regAktifRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<ILayananRepo> _layananRepo = new();
    private readonly Mock<ISequencerManual> _sequencer = new();
    private readonly IOptions<LabOptions> _labOptions = Options.Create(new LabOptions { LayananLabId = "LAB" });
    private readonly TestTglJamProvider _tglJamProvider = new(new DateTime(2026, 5, 20, 9, 30, 0));
    private readonly LabRegIntegration _sut;

    private readonly DateOnly _today = new(2026, 5, 20);

    public LabRegIntegrationTest()
    {
        _sut = new LabRegIntegration(
            _labOrderRepo.Object,
            _regAktifRepo.Object,
            _regRepo.Object,
            _layananRepo.Object,
            _sequencer.Object,
            _labOptions,
            _tglJamProvider);
    }

    private static RegModel CreateSampleReg(
        string regId = "REG-ORIG-01",
        string patientId = "MR0001",
        string tipeJaminanId = "00000",
        string tipeJaminanName = "Bayar Sendiri",
        string dokterId = "DOC01",
        string dokterName = "Dr. Sp.PK")
    {
        return new RegModel(
            regId,
            new DateOnly(2026, 5, 10),
            new AuditInfoType("tester", new DateTime(2026, 5, 10, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            new PasienReff(patientId, "Pasien Tes", new DateOnly(1990, 1, 1), "L"),
            new TipeJaminanReff(tipeJaminanId, tipeJaminanName),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            new PpaReff(dokterId, dokterName),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    private static LabOrderModel CreateEmrOrder(
        string orderId = "LBO-EMR-01",
        string regId = "REG-ORIG-01",
        string patientId = "MR0001",
        string dokterPerujukId = "DOC-REF-01",
        string dokterPerujukName = "Dr. Perujuk")
    {
        var snapshot = new PatientSnapshotType(regId, patientId, "Pasien Tes", new DateTime(1990, 1, 1), "L", 36);
        var item = LabOrderTestSupport.SampleItem();
        return LabOrderModel.CreateFromEmr(
            emrOrderId: "EMR-001",
            snapshot: snapshot,
            lines: [LabOrderTestSupport.ResolvedLine(item)],
            orderNo: "ORD-001",
            audit: new AuditInfoType("U1", new DateTime(2026, 5, 18, 10, 0, 0)),
            dokterPerujukId: dokterPerujukId,
            dokterPerujukName: dokterPerujukName);
    }

    private static LabOrderModel CreateExternalOrder(
        string regExternalId = "REG-EXT-999",
        string patientId = "MR-EXT-01")
    {
        var snapshot = new PatientSnapshotType(regExternalId, patientId, "Pasien Luar", new DateTime(1985, 3, 10), "L", 41);
        var item = LabOrderTestSupport.SampleItem();
        return LabOrderModel.CreateExternal(
            emrOrderId: "EXT-001",
            snapshot: snapshot,
            lines: [LabOrderTestSupport.ResolvedLine(item)],
            orderNo: "ORD-EXT-001",
            audit: new AuditInfoType("U1", new DateTime(2026, 5, 18, 10, 0, 0)));
    }

    [Fact]
    public void CreateExecutionRegistration_GivenExternalPatient_ReturnsRegExternalId()
    {
        // Arrange
        var order = CreateExternalOrder(regExternalId: "REG-EXT-999", patientId: "MR-EXT-01");
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        var request = new LabRegExecutionRequest(order.OrderId, "U1");

        // Act
        var result = _sut.CreateExecutionRegistration(request);

        // Assert
        result.Should().Be("REG-EXT-999");
        _regRepo.Verify(r => r.SaveChanges(It.IsAny<RegModel>()), Times.Never);
        _regAktifRepo.Verify(r => r.SaveChanges(It.IsAny<RegAktifModel>()), Times.Never);
    }

    [Fact]
    public void CreateExecutionRegistration_GivenEmrPatient_WithExplicitValidExecutionRegId_ReturnsExecutionRegId()
    {
        // Arrange
        var order = CreateEmrOrder();
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        var regAktif = new RegAktifModel(
            regId: "REG-TODAY-02",
            regDate: _today,
            pasien: new PasienReff(order.Patient.PatientId, order.Patient.PatientName, new DateOnly(1990, 1, 1), "L"),
            jenisReg: JenisRegEnum.RegJalan,
            layanan: new LayananReff("POLI", "Poli Umum"),
            dokter: new PpaReff("D1", "Dokter 1"),
            tipeJaminan: TipeJaminanType.BayarSendiri.ToReff());

        _regAktifRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-TODAY-02")))
            .Returns(MayBe.From(regAktif));

        var request = new LabRegExecutionRequest(order.OrderId, "U1", "REG-TODAY-02");

        // Act
        var result = _sut.CreateExecutionRegistration(request);

        // Assert
        result.Should().Be("REG-TODAY-02");
        _regRepo.Verify(r => r.SaveChanges(It.IsAny<RegModel>()), Times.Never);
        _regAktifRepo.Verify(r => r.SaveChanges(It.IsAny<RegAktifModel>()), Times.Never);
    }

    [Fact]
    public void CreateExecutionRegistration_GivenEmrPatient_WithExplicitInvalidExecutionRegId_ThrowsValidationException()
    {
        // Arrange
        var order = CreateEmrOrder();
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        _regAktifRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-EXPIRED")))
            .Returns(MayBe<RegAktifModel>.None);

        var request = new LabRegExecutionRequest(order.OrderId, "U1", "REG-EXPIRED");

        // Act
        var act = () => _sut.CreateExecutionRegistration(request);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*tidak aktif hari ini untuk pasien*");
    }

    [Fact]
    public void CreateExecutionRegistration_GivenEmrPatient_WithSingleActiveRegistrationToday_ReturnsExistingRegId()
    {
        // Arrange
        var order = CreateEmrOrder();
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        var activeReg = new RegAktifModel(
            regId: "REG-ACTIVE-01",
            regDate: _today,
            pasien: new PasienReff(order.Patient.PatientId, order.Patient.PatientName, new DateOnly(1990, 1, 1), "L"),
            jenisReg: JenisRegEnum.RegJalan,
            layanan: new LayananReff("POLI", "Poli Umum"),
            dokter: new PpaReff("D1", "Dokter 1"),
            tipeJaminan: TipeJaminanType.BayarSendiri.ToReff());

        _regAktifRepo.Setup(r => r.ListData(It.Is<IPasienKey>(k => k.PasienId == order.Patient.PatientId)))
            .Returns([activeReg]);

        var request = new LabRegExecutionRequest(order.OrderId, "U1");

        // Act
        var result = _sut.CreateExecutionRegistration(request);

        // Assert
        result.Should().Be("REG-ACTIVE-01");
        _regRepo.Verify(r => r.SaveChanges(It.IsAny<RegModel>()), Times.Never);
        _regAktifRepo.Verify(r => r.SaveChanges(It.IsAny<RegAktifModel>()), Times.Never);
    }

    [Fact]
    public void CreateExecutionRegistration_GivenEmrPatient_WithMultipleActiveRegistrationsToday_ThrowsValidationException()
    {
        // Arrange
        var order = CreateEmrOrder();
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        var reg1 = new RegAktifModel("REG-01", _today, new PasienReff(order.Patient.PatientId, "Pasien", new DateOnly(1990, 1, 1), "L"),
            JenisRegEnum.RegJalan, new LayananReff("L1", "Layanan 1"), new PpaReff("D1", "Dokter 1"), TipeJaminanType.BayarSendiri.ToReff());
        var reg2 = new RegAktifModel("REG-02", _today, new PasienReff(order.Patient.PatientId, "Pasien", new DateOnly(1990, 1, 1), "L"),
            JenisRegEnum.RegJalan, new LayananReff("L2", "Layanan 2"), new PpaReff("D2", "Dokter 2"), TipeJaminanType.BayarSendiri.ToReff());

        _regAktifRepo.Setup(r => r.ListData(It.Is<IPasienKey>(k => k.PasienId == order.Patient.PatientId)))
            .Returns([reg1, reg2]);

        var request = new LabRegExecutionRequest(order.OrderId, "U1");

        // Act
        var act = () => _sut.CreateExecutionRegistration(request);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("Terdapat lebih dari satu registrasi aktif hari ini untuk pasien. Silakan pilih nomor registrasi kunjungan.");
    }

    [Fact]
    public void CreateExecutionRegistration_GivenEmrPatient_WithZeroActiveRegistrationsToday_AndBpjs_ThrowsValidationException()
    {
        // Arrange
        var order = CreateEmrOrder();
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        _regAktifRepo.Setup(r => r.ListData(It.Is<IPasienKey>(k => k.PasienId == order.Patient.PatientId)))
            .Returns([]);

        var originalRegBpjs = CreateSampleReg(
            regId: order.Patient.RegId,
            patientId: order.Patient.PatientId,
            tipeJaminanId: "BPJS01",
            tipeJaminanName: "BPJS Kesehatan");

        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == order.Patient.RegId)))
            .Returns(MayBe.From(originalRegBpjs));

        var request = new LabRegExecutionRequest(order.OrderId, "U1");

        // Act
        var act = () => _sut.CreateExecutionRegistration(request);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("Pasien BPJS/Asuransi wajib melakukan pendaftaran di Loket Admisi terlebih dahulu untuk penerbitan eligibilitas (SEP).");
        _regRepo.Verify(r => r.SaveChanges(It.IsAny<RegModel>()), Times.Never);
        _regAktifRepo.Verify(r => r.SaveChanges(It.IsAny<RegAktifModel>()), Times.Never);
    }

    [Fact]
    public void CreateExecutionRegistration_GivenEmrPatient_WithZeroActiveRegistrationsToday_AndUmum_CreatesNewRegistrationAndReturnsNewRegId()
    {
        // Arrange
        var order = CreateEmrOrder(
            dokterPerujukId: "DOC-REF-99",
            dokterPerujukName: "Dr. Sp.PK Perujuk");

        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        _regAktifRepo.Setup(r => r.ListData(It.Is<IPasienKey>(k => k.PasienId == order.Patient.PatientId)))
            .Returns([]);

        var originalRegUmum = CreateSampleReg(
            regId: order.Patient.RegId,
            patientId: order.Patient.PatientId,
            tipeJaminanId: "00000",
            tipeJaminanName: "Bayar Sendiri");

        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == order.Patient.RegId)))
            .Returns(MayBe.From(originalRegUmum));

        _sequencer.Setup(s => s.GetNextNoUrut("NOREG", "No Urut Reg Masuk"))
            .Returns(456L);

        var labLayanan = new LayananType(
            "LAB", "Laboratorium Sentral", true,
            InstalasiType.Default.ToReff(),
            LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default,
            InstalasiDkType.Default,
            Bilreg.Domain.AccountingContext.UnitFeature.UnitType.Default.ToReff(),
            new PoliBpjsReff("-", "-"));

        _layananRepo.Setup(l => l.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LAB")))
            .Returns(MayBe.From(labLayanan));

        RegModel? savedReg = null;
        _regRepo.Setup(r => r.SaveChanges(It.IsAny<RegModel>()))
            .Callback<RegModel>(m => savedReg = m);

        RegAktifModel? savedRegAktif = null;
        _regAktifRepo.Setup(r => r.SaveChanges(It.IsAny<RegAktifModel>()))
            .Callback<RegAktifModel>(m => savedRegAktif = m);

        var request = new LabRegExecutionRequest(order.OrderId, "U1");

        // Act
        var result = _sut.CreateExecutionRegistration(request);

        // Assert
        result.Should().Be("RG00000456");

        savedReg.Should().NotBeNull();
        savedReg!.RegId.Should().Be("RG00000456");
        savedReg.RegDate.Should().Be(_today);
        savedReg.Pasien.PasienId.Should().Be(order.Patient.PatientId);
        savedReg.Layanan.LayananId.Should().Be("LAB");
        savedReg.Layanan.LayananName.Should().Be("Laboratorium Sentral");
        savedReg.Dokter.PpaId.Should().Be("DOC-REF-99");
        savedReg.Dokter.PpaName.Should().Be("Dr. Sp.PK Perujuk");
        savedReg.Karcis.KarcisId.Should().Be("-");
        savedReg.TipeJaminan.TipeJaminanId.Should().Be("00000");

        savedRegAktif.Should().NotBeNull();
        savedRegAktif!.RegId.Should().Be("RG00000456");
        savedRegAktif.RegDate.Should().Be(_today);
        savedRegAktif.Pasien.PasienId.Should().Be(order.Patient.PatientId);
        savedRegAktif.Layanan.LayananId.Should().Be("LAB");
        savedRegAktif.Dokter.PpaId.Should().Be("DOC-REF-99");

        _regRepo.Verify(r => r.SaveChanges(It.IsAny<RegModel>()), Times.Once);
        _regAktifRepo.Verify(r => r.SaveChanges(It.IsAny<RegAktifModel>()), Times.Once);
    }
}
