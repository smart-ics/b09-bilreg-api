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

public class RegRelationPolicyAdapterTest
{
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly RegRelationPolicyAdapter _sut;

    public RegRelationPolicyAdapterTest()
    {
        _sut = new RegRelationPolicyAdapter(_regRepoMock.Object);
    }

    private static RegModel CreateReg(string regId, string patientId, bool isVoided = false)
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
            new PasienReff(patientId, "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            TipeJaminanType.Default.ToReff(),
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

    [Fact]
    public void GivenMatchingPatientAndActiveRegistrations_WhenIsRelated_ThenReturnsTrue()
    {
        // Arrange
        var sourceReg = CreateReg("REG-01", "PAT-001");
        var targetReg = CreateReg("REG-02", "PAT-001");

        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(sourceReg));
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-02")))
            .Returns(MayBe.From(targetReg));

        // Act
        var result = _sut.IsRelated("REG-01", "REG-02");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GivenDifferentPatients_WhenIsRelated_ThenReturnsFalse()
    {
        // Arrange
        var sourceReg = CreateReg("REG-01", "PAT-001");
        var targetReg = CreateReg("REG-02", "PAT-002");

        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(sourceReg));
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-02")))
            .Returns(MayBe.From(targetReg));

        // Act
        var result = _sut.IsRelated("REG-01", "REG-02");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenSourceNotFound_WhenIsRelated_ThenReturnsFalse()
    {
        // Arrange
        var targetReg = CreateReg("REG-02", "PAT-001");

        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe<RegModel>.None);
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-02")))
            .Returns(MayBe.From(targetReg));

        // Act
        var result = _sut.IsRelated("REG-01", "REG-02");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenTargetNotFound_WhenIsRelated_ThenReturnsFalse()
    {
        // Arrange
        var sourceReg = CreateReg("REG-01", "PAT-001");

        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(sourceReg));
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-02")))
            .Returns(MayBe<RegModel>.None);

        // Act
        var result = _sut.IsRelated("REG-01", "REG-02");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenSourceVoided_WhenIsRelated_ThenReturnsFalse()
    {
        // Arrange
        var sourceReg = CreateReg("REG-01", "PAT-001", isVoided: true);
        var targetReg = CreateReg("REG-02", "PAT-001", isVoided: false);

        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(sourceReg));
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-02")))
            .Returns(MayBe.From(targetReg));

        // Act
        var result = _sut.IsRelated("REG-01", "REG-02");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenTargetVoided_WhenIsRelated_ThenReturnsFalse()
    {
        // Arrange
        var sourceReg = CreateReg("REG-01", "PAT-001", isVoided: false);
        var targetReg = CreateReg("REG-02", "PAT-001", isVoided: true);

        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-01")))
            .Returns(MayBe.From(sourceReg));
        _regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-02")))
            .Returns(MayBe.From(targetReg));

        // Act
        var result = _sut.IsRelated("REG-01", "REG-02");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "REG-02")]
    [InlineData("", "REG-02")]
    [InlineData("   ", "REG-02")]
    [InlineData("REG-01", null)]
    [InlineData("REG-01", "")]
    [InlineData("REG-01", "   ")]
    public void GivenNullOrWhitespaceIds_WhenIsRelated_ThenReturnsFalse(string? sourceRegId, string? targetRegId)
    {
        // Act
        var result = _sut.IsRelated(sourceRegId!, targetRegId!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenSameRegistrationId_WhenIsRelated_ThenReturnsFalse()
    {
        // Act
        var result = _sut.IsRelated("REG-01", "REG-01");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenServiceCollection_WhenAddTataRekeningFeature_ThenRegistersIRegRelationPolicy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRegRepo>(_ => _regRepoMock.Object);

        // Act
        services.AddTataRekeningFeature();
        var provider = services.BuildServiceProvider();
        var policy = provider.GetService<IRegRelationPolicy>();

        // Assert
        policy.Should().NotBeNull();
        policy.Should().BeOfType<RegRelationPolicyAdapter>();
    }
}
