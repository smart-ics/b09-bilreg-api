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
