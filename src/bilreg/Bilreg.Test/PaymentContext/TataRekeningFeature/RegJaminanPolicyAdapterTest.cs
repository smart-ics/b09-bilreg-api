using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class RegJaminanPolicyAdapterTest
{
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<ITipeJaminanRepo> _tipeJaminanRepoMock = new();
    private readonly RegJaminanPolicyAdapter _sut;

    public RegJaminanPolicyAdapterTest()
    {
        _sut = new RegJaminanPolicyAdapter(_regRepoMock.Object, _tipeJaminanRepoMock.Object);
    }

    private static RegModel CreateReg(
        string regId,
        string tipeJaminanId = "BPJS",
        string tipeJaminanName = "BPJS Kesehatan",
        bool isVoided = false)
    {
        var voidAudit = isVoided
            ? new AuditInfoType("void-user", new DateTime(2026, 6, 15, 10, 0, 0))
            : AuditInfoType.Default;

        return new RegModel(
            regId,
            new DateOnly(2026, 6, 14),
            new AuditInfoType("tester", new DateTime(2026, 6, 14, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            voidAudit,
            JenisRegEnum.RegJalan,
            new PasienReff("PAT-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new TipeJaminanReff(tipeJaminanId, tipeJaminanName),
            PolisModel.Default.ToReff(),
            new KelasReff("-", "-"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    private static TipeJaminanType CreateTipeJaminan(
        string id = "BPJS",
        string name = "BPJS Kesehatan",
        bool isAktif = true)
    {
        return new TipeJaminanType(
            id,
            name,
            isAktif,
            JaminanType.Default.ToReff(),
            CaraBayarDkType.Default);
    }

    [Fact]
    public void GivenMatchingInsuranceAndActiveRegistration_WhenIsJaminanValidForReg_ThenReturnsTrue()
    {
        // Arrange
        var reg = CreateReg("REG-01", "BPJS");
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));
        _tipeJaminanRepoMock.Setup(r => r.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "BPJS")))
            .Returns(MayBe.From(CreateTipeJaminan("BPJS", "BPJS Kesehatan", isAktif: true)));

        // Act
        var result = _sut.IsJaminanValidForReg("REG-01", "BPJS");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GivenMatchingInsuranceWithDifferentCasing_WhenIsJaminanValidForReg_ThenReturnsTrueCaseInsensitive()
    {
        // Arrange
        var reg = CreateReg("REG-01", "BPJS");
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));
        _tipeJaminanRepoMock.Setup(r => r.LoadEntity(It.Is<ITipeJaminanKey>(k => string.Equals(k.TipeJaminanId, "bpjs", StringComparison.OrdinalIgnoreCase))))
            .Returns(MayBe.From(CreateTipeJaminan("BPJS", "BPJS Kesehatan", isAktif: true)));

        // Act
        var result = _sut.IsJaminanValidForReg("REG-01", "bpjs");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GivenSecondaryActiveInsuranceDifferentFromRegistration_WhenIsJaminanValidForReg_ThenReturnsTrue()
    {
        // Arrange: registration has BPJS, but payment is secondary insurance ASR-PRUDENTIAL which is active in master
        var reg = CreateReg("REG-01", "BPJS");
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));
        _tipeJaminanRepoMock.Setup(r => r.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "ASR-PRUDENTIAL")))
            .Returns(MayBe.From(CreateTipeJaminan("ASR-PRUDENTIAL", "Prudential Life", isAktif: true)));

        // Act
        var result = _sut.IsJaminanValidForReg("REG-01", "ASR-PRUDENTIAL");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GivenPaymentId00000_WhenIsJaminanValidForReg_ThenReturnsFalse()
    {
        // Arrange: "00000" (Bayar Sendiri / UMUM) must always be rejected as insurance
        var reg = CreateReg("REG-01", "00000", "Membayar Sendiri");
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));

        // Act
        var result = _sut.IsJaminanValidForReg("REG-01", "00000");

        // Assert
        result.Should().BeFalse();
        _tipeJaminanRepoMock.Verify(r => r.LoadEntity(It.IsAny<ITipeJaminanKey>()), Times.Never);
    }

    [Fact]
    public void GivenInactiveInsurance_WhenIsJaminanValidForReg_ThenReturnsFalse()
    {
        // Arrange: insurance exists in master but IsAktif == false
        var reg = CreateReg("REG-01", "BPJS");
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));
        _tipeJaminanRepoMock.Setup(r => r.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "INACTIVE-JMN")))
            .Returns(MayBe.From(CreateTipeJaminan("INACTIVE-JMN", "Asuransi Non-Aktif", isAktif: false)));

        // Act
        var result = _sut.IsJaminanValidForReg("REG-01", "INACTIVE-JMN");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenInsuranceNotFoundInMaster_WhenIsJaminanValidForReg_ThenReturnsFalse()
    {
        // Arrange
        var reg = CreateReg("REG-01", "BPJS");
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));
        _tipeJaminanRepoMock.Setup(r => r.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "NON-EXISTENT")))
            .Returns(MayBe<TipeJaminanType>.None);

        // Act
        var result = _sut.IsJaminanValidForReg("REG-01", "NON-EXISTENT");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenRegistrationNotFound_WhenIsJaminanValidForReg_ThenReturnsFalse()
    {
        // Arrange
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe<RegModel>.None);

        // Act
        var result = _sut.IsJaminanValidForReg("REG-01", "BPJS");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenVoidedRegistration_WhenIsJaminanValidForReg_ThenReturnsFalse()
    {
        // Arrange
        var reg = CreateReg("REG-01", "BPJS", isVoided: true);
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));

        // Act
        var result = _sut.IsJaminanValidForReg("REG-01", "BPJS");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "BPJS")]
    [InlineData("", "BPJS")]
    [InlineData("   ", "BPJS")]
    [InlineData("REG-01", null)]
    [InlineData("REG-01", "")]
    [InlineData("REG-01", "   ")]
    public void GivenNullOrWhitespaceInputs_WhenIsJaminanValidForReg_ThenReturnsFalse(string? regId, string? paymentId)
    {
        // Act
        var result = _sut.IsJaminanValidForReg(regId!, paymentId!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenEmptyRegistrationIdInEntity_WhenIsJaminanValidForReg_ThenReturnsFalse()
    {
        // Arrange
        var reg = CreateReg("-", "BPJS");
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(reg));

        // Act
        var result = _sut.IsJaminanValidForReg("REG-01", "BPJS");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenNullRegRepo_WhenConstructingAdapter_ThenThrowsArgumentNullException()
    {
        // Act
        var act = () => new RegJaminanPolicyAdapter(null!, _tipeJaminanRepoMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("regRepo");
    }

    [Fact]
    public void GivenNullTipeJaminanRepo_WhenConstructingAdapter_ThenThrowsArgumentNullException()
    {
        // Act
        var act = () => new RegJaminanPolicyAdapter(_regRepoMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tipeJaminanRepo");
    }

    [Fact]
    public void GivenServiceCollection_WhenAddTataRekeningFeature_ThenRegistersIRegJaminanPolicy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRegRepo>(_ => _regRepoMock.Object);
        services.AddScoped<ITipeJaminanRepo>(_ => _tipeJaminanRepoMock.Object);

        // Act
        services.AddTataRekeningFeature();
        var provider = services.BuildServiceProvider();
        var policy = provider.GetService<IRegJaminanPolicy>();

        // Assert
        policy.Should().NotBeNull();
        policy.Should().BeOfType<RegJaminanPolicyAdapter>();
    }
}
