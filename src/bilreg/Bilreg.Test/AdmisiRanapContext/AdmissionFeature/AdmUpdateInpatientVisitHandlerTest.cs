using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmUpdateInpatientVisitHandlerTest
{
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IRegInapRepo> _regInapRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IProsedureMasukInapRepo> _prosedurRepoMock = new();
    private Mock<IProsedureMasukInapRepo> _prosedurMasukInapRepoMock => _prosedurRepoMock;
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<IRegAktifRepo> _regAktifRepoMock = new();
    private readonly Mock<ICaraMasukDkRepo> _caraMasukRepoMock = new();

    private readonly AdmUpdateInpatientVisitHandler _sut;

    public AdmUpdateInpatientVisitHandlerTest()
    {
        _sut = new AdmUpdateInpatientVisitHandler(
            _regRepoMock.Object,
            _regInapRepoMock.Object,
            _ppaRepoMock.Object,
            _prosedurRepoMock.Object,
            _layananRepoMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance,
            _regAktifRepoMock.Object,
            _caraMasukRepoMock.Object);
    }

    private AdmUpdateInpatientVisitHandler CreateSut() =>
        new(
            _regRepoMock.Object,
            _regInapRepoMock.Object,
            _ppaRepoMock.Object,
            _prosedurMasukInapRepoMock.Object,
            _caraMasukRepoMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance);

    private void SetupMasterData(string dokterId = "DOC01", string prosedurId = "PRO01", string caraMasukId = "CM01")
    {
        _ppaRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == dokterId)))
            .Returns(MayBe.From(PpaType.Default with { PpaId = dokterId, PpaName = $"Dokter {dokterId}" }));

        _prosedurMasukInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IProsedurMasukInapKey>(k => k.ProsedurMasukInapId == prosedurId)))
            .Returns(MayBe.From(new ProsedurMasukInapType(prosedurId, $"Prosedur {prosedurId}", ProsedurMasukInapDkType.Default)));

        _caraMasukRepoMock
            .Setup(x => x.LoadEntity(It.Is<ICaraMasukDkKey>(k => k.CaraMasukDkId == caraMasukId)))
            .Returns(MayBe.From(new CaraMasukDkType(caraMasukId, $"Cara Masuk {caraMasukId}")));
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndRegIbuIsEmpty_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "3", "", "USER01", "CM01");

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Nomor registrasi ibu wajib diisi untuk jenis inap Perinatal*");
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndMotherNotFound_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "3", "REG-IBU-999", "USER01", "CM01");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-IBU-999")))
            .Returns(MayBe<RegModel>.None);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Registrasi ibu 'REG-IBU-999' tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndMotherIsVoid_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "3", "REG-IBU-001", "USER01", "CM01");

        var mother = CreateSampleRegHead("REG-IBU-001", "CM01", "DOC01", jenisInapId: "2",
            regVoidAudit: new AuditInfoType("USER_VOID", new DateTime(2026, 5, 1)));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-IBU-001")))
            .Returns(MayBe.From(mother));

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registrasi ibu sudah berstatus void.");
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndMotherIsDischarged_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "3", "REG-IBU-001", "USER01", "CM01");

        var mother = CreateSampleRegHead("REG-IBU-001", "CM01", "DOC01", jenisInapId: "2",
            regKeluarAudit: new AuditInfoType("USER_OUT", new DateTime(2026, 5, 1)));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-IBU-001")))
            .Returns(MayBe.From(mother));

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registrasi ibu sudah berstatus pulang/keluar.");
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndMotherNotObstetri_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "3", "REG-IBU-001", "USER01", "CM01");

        var mother = CreateSampleRegHead("REG-IBU-001", "CM01", "DOC01", jenisInapId: "1"); // 1 = Umum

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-IBU-001")))
            .Returns(MayBe.From(mother));

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("JENIS INAP IBU invalid: Registrasi ibu harus berjenis inap Obstetri.");
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndMotherValid_SuccessAndPersistsBothRegs()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC02", "PRO02", "3", "REG-IBU-001", "USER01", "CM02");

        var mother = CreateSampleRegHead("REG-IBU-001", "CM01", "DOC01", jenisInapId: "2");
        var patientReg = CreateSampleRegHead("REG001", "CM01", "DOC01", jenisInapId: "1");
        var patientRegInap = CreateSampleRegInapHead("REG001", "PRO01", "DOC01");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-IBU-001")))
            .Returns(MayBe.From(mother));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientReg));

        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientRegInap));

        SetupMasterData("DOC02", "PRO02", "CM02");

        RegModel? savedReg = null;
        RegInapModel? savedRegInap = null;

        _regRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<RegModel>()))
            .Callback<RegModel>(m => savedReg = m);

        _regInapRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<RegInapModel>()))
            .Callback<RegInapModel>(m => savedRegInap = m);

        await sut.Handle(cmd, CancellationToken.None);

        savedReg.Should().NotBeNull();
        savedReg!.JenisInap.JenisInapId.Should().Be("3");
        savedReg.RegIbu.Should().Be("REG-IBU-001");
        savedReg.CaraMasukDk.CaraMasukDkId.Should().Be("CM02");
        savedReg.Dokter.PpaId.Should().Be("DOC02");

        savedRegInap.Should().NotBeNull();
        savedRegInap!.ProsedurMasukInap.ProsedurMasukInapId.Should().Be("PRO02");
        savedRegInap.Dpjp.PpaId.Should().Be("DOC02");

        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenNonPerinatal_ClearsRegIbuAndPersistsSuccess()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "1", "OLD-IBU", "USER01", "CM01");

        var patientReg = CreateSampleRegHead("REG001", "CM01", "DOC01", jenisInapId: "3", regIbu: "OLD-IBU");
        var patientRegInap = CreateSampleRegInapHead("REG001", "PRO01", "DOC01");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientReg));

        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientRegInap));

        SetupMasterData("DOC01", "PRO01", "CM01");

        RegModel? savedReg = null;
        _regRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<RegModel>()))
            .Callback<RegModel>(m => savedReg = m);

        await sut.Handle(cmd, CancellationToken.None);

        savedReg.Should().NotBeNull();
        savedReg!.JenisInap.JenisInapId.Should().Be("1");
        savedReg.RegIbu.Should().BeEmpty();
        _regRepoMock.Verify(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "OLD-IBU")), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDpjpUnchanged_DoesNotThrowDuplicatePrimaryAndUpdatesProsedur()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO02", "1", "", "USER01", "CM01");

        var patientReg = CreateSampleRegHead("REG001", "CM01", "DOC01", jenisInapId: "1");
        var patientRegInap = CreateSampleRegInapHead("REG001", "PRO01", "DOC01");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientReg));

        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientRegInap));

        SetupMasterData("DOC01", "PRO02", "CM01");

        RegInapModel? savedRegInap = null;
        _regInapRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<RegInapModel>()))
            .Callback<RegInapModel>(m => savedRegInap = m);

        await sut.Handle(cmd, CancellationToken.None);

        savedRegInap.Should().NotBeNull();
        savedRegInap!.Dpjp.PpaId.Should().Be("DOC01");
        savedRegInap.ProsedurMasukInap.ProsedurMasukInapId.Should().Be("PRO02");
    }

    [Fact]
    public async Task Handle_WhenTargetRegNotFound_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG_MISSING", "DOC01", "PRO01", "1", "", "USER01", "CM01");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_MISSING")))
            .Returns(MayBe<RegModel>.None);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Registrasi 'REG_MISSING' tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_WhenTargetRegInapNotFound_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "1", "", "USER01", "CM01");

        var patientReg = CreateSampleRegHead("REG001", "CM01", "DOC01", jenisInapId: "1");
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientReg));

        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe<RegInapModel>.None);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Registrasi rawat inap 'REG001' tidak ditemukan*");
    }

    private static RegModel CreateSampleRegHead(
        string regId,
        string caraMasukDkId,
        string dokterId,
        string? jenisInapId = null,
        string? regIbu = null,
        AuditInfoType? regVoidAudit = null,
        AuditInfoType? regKeluarAudit = null) =>
        new(
            regId,
            new DateOnly(2025, 5, 1),
            AuditInfoType.Default,
            regKeluarAudit ?? AuditInfoType.Default,
            AuditInfoType.Default,
            regVoidAudit ?? AuditInfoType.Default,
            JenisRegEnum.RegInap,
            new PasienReff("PAS001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            new CaraMasukDkType(caraMasukDkId, "Cara Masuk Test"),
            RujukanType.Default.ToReff(),
            new PpaReff(dokterId, "Dokter Test"),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            [],
            jenisInap: jenisInapId is null ? null : new JenisInapType(jenisInapId, "Test"),
            regIbu: regIbu);

    private static RegInapModel CreateSampleRegInapHead(string regId, string prosedurMasukInapId, string dpjpId) =>
        RegInapModel.Create(
            regId,
            new ProsedurMasukInapType(prosedurMasukInapId, "Prosedur Test", ProsedurMasukInapDkType.Default),
            new PpaReff(dpjpId, "DPJP Test"),
            new DateOnly(2025, 5, 1));

    [Fact]
    public async Task UT01_GivenValidRequestWithNewDoctor_WhenHandled_UpdatesDpjpAndVisitAndSavesAudit()
    {
        // Arrange
        var reg = CreateSampleReg("RG001", "DK1", "L1", isAktif: true);
        var regInap = CreateSampleRegInap("RG001", "DK1", "P1");

        SetupDefaults(reg, regInap);

        var newDokter = PpaType.Default with { PpaId = "DK2", PpaName = "Dr. Dua" };
        _ppaRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DK2")))
            .Returns(MayBe.From(newDokter));

        var newProsedur = new ProsedurMasukInapType("P2", "Rujukan Luar", ProsedurMasukInapDkType.Default);
        _prosedurRepoMock
            .Setup(x => x.LoadEntity(It.Is<IProsedurMasukInapKey>(k => k.ProsedurMasukInapId == "P2")))
            .Returns(MayBe.From(newProsedur));

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK2", "P2", "1", "", "user2");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        regInap.Dpjp.PpaId.Should().Be("DK2");
        regInap.ProsedurMasukInap.ProsedurMasukInapId.Should().Be("P2");
        reg.Dokter.PpaId.Should().Be("DK2");
        reg.JenisInap.JenisInapId.Should().Be("1");

        _regRepoMock.Verify(x => x.SaveChanges(reg), Times.Once);
        _regInapRepoMock.Verify(x => x.SaveChanges(regInap), Times.Once);
        _regAktifRepoMock.Verify(x => x.SaveChanges(It.IsAny<RegAktifModel>()), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "UPDATE"
            && a.EntityName == nameof(RegInapModel)
            && a.EntityId == "RG001"
            && a.OriginalDataJson != null)), Times.Once);
    }

    [Fact]
    public async Task UT02_GivenValidRequestWithSameDoctor_WhenHandled_DoesNotChangePrimaryDpjp()
    {
        // Arrange
        var reg = CreateSampleReg("RG001", "DK1", "L1", isAktif: true);
        var regInap = CreateSampleRegInap("RG001", "DK1", "P1");

        SetupDefaults(reg, regInap);

        var newProsedur = new ProsedurMasukInapType("P2", "Rujukan Luar", ProsedurMasukInapDkType.Default);
        _prosedurRepoMock
            .Setup(x => x.LoadEntity(It.Is<IProsedurMasukInapKey>(k => k.ProsedurMasukInapId == "P2")))
            .Returns(MayBe.From(newProsedur));

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P2", "2", "", "user2");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert - no exception thrown by ChangePrimaryDpjp
        regInap.Dpjp.PpaId.Should().Be("DK1");
        regInap.ProsedurMasukInap.ProsedurMasukInapId.Should().Be("P2");
        reg.Dokter.PpaId.Should().Be("DK1");
        reg.JenisInap.JenisInapId.Should().Be("2");
    }

    [Fact]
    public async Task UT03_GivenPerinatalWithoutRegIbu_WhenHandled_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P1", "3", "  ", "user2");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Reg Ibu*");
    }

    [Fact]
    public async Task UT04_GivenPerinatalWithRegIbu_WhenHandled_SucceedsAndSetsRegIbu()
    {
        // Arrange
        var reg = CreateSampleReg("RG001", "DK1", "L1", isAktif: true);
        var regInap = CreateSampleRegInap("RG001", "DK1", "P1");

        SetupDefaults(reg, regInap);

        var mother = CreateSampleReg("RG-IBU-001", "DK1", "L1", isAktif: true, jenisInapId: "2");
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "RG-IBU-001")))
            .Returns(MayBe.From(mother));

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P1", "3", "RG-IBU-001", "user2");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        reg.JenisInap.JenisInapId.Should().Be("3");
        reg.RegIbu.Should().Be("RG-IBU-001");
    }

    [Fact]
    public async Task UT05_GivenInvalidJenisInap_WhenHandled_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P1", "99", "", "user2");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak valid*");
    }

    [Fact]
    public async Task UT06_GivenRegistrationNotFound_WhenHandled_ThrowsKeyNotFoundException()
    {
        // Arrange
        _regRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P1", "1", "", "user2");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task UT07_GivenInactiveRegistration_WhenHandled_ThrowsInvalidOperationException()
    {
        // Arrange
        var reg = CreateSampleReg("RG001", "DK1", "L1", isAktif: false);
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "RG001")))
            .Returns(MayBe.From(reg));

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P1", "1", "", "user2");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak aktif*");
    }

    [Fact]
    public async Task UT08_GivenRegInapNotFound_WhenHandled_ThrowsKeyNotFoundException()
    {
        // Arrange
        var reg = CreateSampleReg("RG001", "DK1", "L1", isAktif: true);
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "RG001")))
            .Returns(MayBe.From(reg));
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegInapModel>.None);

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P1", "1", "", "user2");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task UT09_GivenTerminalRegInap_WhenHandled_ThrowsInvalidOperationException()
    {
        // Arrange
        var reg = CreateSampleReg("RG001", "DK1", "L1", isAktif: true);
        var regInap = CreateSampleRegInap("RG001", "DK1", "P1");
        regInap.EndAllDoctorAssignments(new DateOnly(2025, 5, 2));

        SetupDefaults(reg, regInap);

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P1", "1", "", "user2");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*terminal*");
    }

    [Fact]
    public async Task UT10_GivenDokterNotFound_WhenHandled_ThrowsKeyNotFoundException()
    {
        // Arrange
        var reg = CreateSampleReg("RG001", "DK1", "L1", isAktif: true);
        var regInap = CreateSampleRegInap("RG001", "DK1", "P1");

        SetupDefaults(reg, regInap);

        _ppaRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DK_MISSING")))
            .Returns(MayBe<PpaType>.None);

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK_MISSING", "P1", "1", "", "user2");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task UT11_GivenProsedurNotFound_WhenHandled_ThrowsKeyNotFoundException()
    {
        // Arrange
        var reg = CreateSampleReg("RG001", "DK1", "L1", isAktif: true);
        var regInap = CreateSampleRegInap("RG001", "DK1", "P1");

        SetupDefaults(reg, regInap);

        _prosedurRepoMock
            .Setup(x => x.LoadEntity(It.Is<IProsedurMasukInapKey>(k => k.ProsedurMasukInapId == "P_MISSING")))
            .Returns(MayBe<ProsedurMasukInapType>.None);

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P_MISSING", "1", "", "user2");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task UT12_GivenLayananNotFound_WhenHandled_ThrowsKeyNotFoundException()
    {
        // Arrange
        var reg = CreateSampleReg("RG001", "DK1", "L_MISSING", isAktif: true);
        var regInap = CreateSampleRegInap("RG001", "DK1", "P1");

        SetupDefaults(reg, regInap);

        _layananRepoMock
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L_MISSING")))
            .Returns(MayBe<LayananType>.None);

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P1", "1", "", "user2");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task UT13_GivenLayananNotRawatInap_WhenHandled_ThrowsArgumentException()
    {
        // Arrange
        var reg = CreateSampleReg("RG001", "DK1", "L_RAJAL", isAktif: true);
        var regInap = CreateSampleRegInap("RG001", "DK1", "P1");

        SetupDefaults(reg, regInap);

        var rajalLayanan = LayananType.Default with
        {
            LayananId = "L_RAJAL",
            InstalasiDk = new InstalasiDkType("2", "Rawat Jalan")
        };
        _layananRepoMock
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L_RAJAL")))
            .Returns(MayBe.From(rajalLayanan));

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P1", "1", "", "user2");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*bukan instalasi rawat inap*");
    }

    [Theory]
    [InlineData("", "DK1", "P1", "1", "user1")]
    [InlineData("RG001", "", "P1", "1", "user1")]
    [InlineData("RG001", "DK1", "", "1", "user1")]
    [InlineData("RG001", "DK1", "P1", "", "user1")]
    [InlineData("RG001", "DK1", "P1", "1", "")]
    public async Task UT14_GivenGuardClauses_WhenAnyRequiredFieldNullOrWhiteSpace_Throws(
        string regId, string dokterId, string prosedurId, string jenisInapId, string userId)
    {
        // Arrange
        var cmd = new AdmUpdateInpatientVisitCmd(regId, dokterId, prosedurId, jenisInapId, "", userId);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UT15_GivenValidRequestWithCaraMasuk_WhenHandled_UpdatesCaraMasukOnReg()
    {
        // Arrange
        var reg = CreateSampleReg("RG001", "DK1", "L1", isAktif: true);
        var regInap = CreateSampleRegInap("RG001", "DK1", "P1");
        SetupDefaults(reg, regInap);

        var caraMasuk = new CaraMasukDkType("8", "DATANG SENDIRI");
        _caraMasukRepoMock
            .Setup(x => x.LoadEntity(It.Is<ICaraMasukDkKey>(k => k.CaraMasukDkId == "8")))
            .Returns(MayBe.From(caraMasuk));

        var cmd = new AdmUpdateInpatientVisitCmd("RG001", "DK1", "P1", "1", "", "user1", "8");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        reg.CaraMasukDk.CaraMasukDkId.Should().Be("8");
        _regRepoMock.Verify(x => x.SaveChanges(reg), Times.Once);
    }

    private void SetupDefaults(RegModel reg, RegInapModel regInap)
    {
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == reg.RegId)))
            .Returns(MayBe.From(reg));

        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == reg.RegId)))
            .Returns(MayBe.From(regInap));

        var dokter = PpaType.Default with { PpaId = "DK1", PpaName = "Dr. Satu" };
        _ppaRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DK1")))
            .Returns(MayBe.From(dokter));

        var prosedur = new ProsedurMasukInapType("P1", "Prosedur 1", ProsedurMasukInapDkType.Default);
        _prosedurRepoMock
            .Setup(x => x.LoadEntity(It.Is<IProsedurMasukInapKey>(k => k.ProsedurMasukInapId == "P1")))
            .Returns(MayBe.From(prosedur));

        var layanan = LayananType.Default with
        {
            LayananId = "L1",
            InstalasiDk = InstalasiDkType.RawatInap
        };
        _layananRepoMock
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L1")))
            .Returns(MayBe.From(layanan));
    }

    private static RegModel CreateSampleReg(
        string regId = "RG001",
        string dokterId = "DK1",
        string layananId = "L1",
        bool isAktif = true,
        string jenisInapId = "1",
        string regIbu = "")
    {
        return new RegModel(
            regId,
            new DateOnly(2025, 5, 1),
            new AuditInfoType("user1", new DateTime(2025, 5, 1, 8, 0, 0)),
            isAktif ? AuditInfoType.Default : new AuditInfoType("user1", new DateTime(2025, 5, 2, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegInap,
            new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            new KelasReff("K1", "Kelas 1"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            new PpaReff(dokterId, "Dr. " + dokterId),
            new LayananReff(layananId, "Bangsal Layanan"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            [],
            null,
            null,
            JenisInapType.FromId(jenisInapId),
            regIbu);
    }

    private static RegInapModel CreateSampleRegInap(
        string regId = "RG001",
        string dokterId = "DK1",
        string prosedurId = "P1")
    {
        var prosedur = new ProsedurMasukInapType(prosedurId, "Prosedur 1", ProsedurMasukInapDkType.Default);
        var dokter = new PpaReff(dokterId, "Dr. " + dokterId);
        return RegInapModel.Create(regId, prosedur, dokter, new DateOnly(2025, 5, 1));
    }
}
