using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmGetAdmissionHandlerTest
{
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IRegInapRepo> _regInapRepoMock = new();
    private readonly AdmGetAdmissionHandler _sut;

    public AdmGetAdmissionHandlerTest()
    {
        _sut = new AdmGetAdmissionHandler(
            _admissionRepoMock.Object,
            _regRepoMock.Object,
            _regInapRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAdmissionNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM999")))
            .Returns(MayBe<AdmissionModel>.None);

        var query = new AdmGetAdmissionQry("ADM999");

        // Act
        Func<Task> act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Admission 'ADM999' tidak ditemukan.");
    }

    [Fact]
    public async Task Handle_WhenRegAndRegInapExist_MapsCaraMasukDpjpAndProsedur()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM001");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM001")))
            .Returns(MayBe.From(admission));

        var reg = CreateSampleReg("ADM001", caraMasukDkId: "8", dokterId: "REG_DOK");
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM001")))
            .Returns(MayBe.From(reg));

        var regInap = CreateSampleRegInap("ADM001", prosedurMasukInapId: "1", dpjpId: "DPJP_AKTIF");
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM001")))
            .Returns(MayBe.From(regInap));

        var query = new AdmGetAdmissionQry("ADM001");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be("ADM001");
        result.AdmissionStatus.Should().Be(AdmissionStatusEnum.Admitted);
        result.Pasien.PasienId.Should().Be("PAS001");
        result.CaraMasukDkId.Should().Be("8");
        result.DokterId.Should().Be("DPJP_AKTIF");
        result.ProsedurMasukInapId.Should().Be("1");
    }

    [Fact]
    public async Task Handle_WhenRegInapHasNoDpjp_FallsBackToRegDokter()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM002");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM002")))
            .Returns(MayBe.From(admission));

        var reg = CreateSampleReg("ADM002", caraMasukDkId: "2", dokterId: "FALLBACK_DOK");
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM002")))
            .Returns(MayBe.From(reg));

        // RegInap with no primary DPJP or empty DPJP
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM002")))
            .Returns(MayBe<RegInapModel>.None);

        var query = new AdmGetAdmissionQry("ADM002");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.CaraMasukDkId.Should().Be("2");
        result.DokterId.Should().Be("FALLBACK_DOK");
        result.ProsedurMasukInapId.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenRegAndRegInapNotFound_ReturnsEmptyEnrichedFields()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM003");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM003")))
            .Returns(MayBe.From(admission));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM003")))
            .Returns(MayBe<RegModel>.None);

        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM003")))
            .Returns(MayBe<RegInapModel>.None);

        var query = new AdmGetAdmissionQry("ADM003");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.CaraMasukDkId.Should().BeEmpty();
        result.DokterId.Should().BeEmpty();
        result.ProsedurMasukInapId.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenFieldsContainLegacyDash_NormalizesToEmpty()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM004");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM004")))
            .Returns(MayBe.From(admission));

        var reg = CreateSampleReg("ADM004", caraMasukDkId: "-", dokterId: "-");
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM004")))
            .Returns(MayBe.From(reg));

        var regInap = CreateSampleRegInap("ADM004", prosedurMasukInapId: "-", dpjpId: "-");
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM004")))
            .Returns(MayBe.From(regInap));

        var query = new AdmGetAdmissionQry("ADM004");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.CaraMasukDkId.Should().BeEmpty();
        result.DokterId.Should().BeEmpty();
        result.ProsedurMasukInapId.Should().BeEmpty();
    }

    private static AdmissionModel CreateSampleAdmission(string regId) =>
        new(
            regId,
            AdmissionStatusEnum.Admitted,
            new PasienReff("PAS001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            "-",
            "-",
            new KelasDkType("KDK01", "Kelas 1"),
            new BangsalReff("B01", "Bangsal A"),
            new DateTime(2026, 7, 7, 10, 0, 0),
            AuditTrailType.Create("USER01", new DateTime(2026, 7, 7, 10, 0, 0)));

    private static RegModel CreateSampleReg(string regId, string caraMasukDkId, string dokterId) =>
        new(
            regId,
            new DateOnly(2026, 7, 7),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
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
            []);

    private static RegInapModel CreateSampleRegInap(string regId, string prosedurMasukInapId, string dpjpId) =>
        RegInapModel.Create(
            regId,
            new ProsedurMasukInapType(prosedurMasukInapId, "Prosedur Test", ProsedurMasukInapDkType.Default),
            new PpaReff(dpjpId, "DPJP Test"),
            new DateOnly(2026, 7, 7));
}
