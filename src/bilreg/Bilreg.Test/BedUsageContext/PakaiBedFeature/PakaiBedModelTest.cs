using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedModelTest
{
    private readonly RegModel _defaultReg = RegModel.Default;
    private readonly LayananType _defaultLayanan = LayananType.Default with { LayananId = "LYN01", LayananName = "Bangsal Anggrek" };
    private readonly BedType _defaultBed = BedType.Default with { BedId = "BED01", BedName = "Bed 01" };
    private readonly TipeKamarType _defaultTipeKamar = TipeKamarType.Default with { TipeKamarId = "TK01", TipeKamarName = "Standard" };
    private readonly KelasType _defaultKelas = KelasType.Default with { KelasId = "KLS01", KelasName = "Kelas 1" };
    private readonly AuditInfoType _defaultAudit = new("USR01", new DateTime(2026, 11, 11, 10, 0, 0));

    [Fact]
    public void Create_WithLayananDkId_ShouldSetLayananDkIdProperty()
    {
        // Act
        var sut = PakaiBedModel.Create(
            _defaultReg,
            _defaultAudit,
            _defaultLayanan,
            _defaultBed,
            _defaultTipeKamar,
            _defaultKelas,
            150000m,
            layananDkId: "88");

        // Assert
        sut.LayananDkId.Should().Be("88");
    }

    [Fact]
    public void Create_WithoutLayananDkId_ShouldDefaultToEmptyString()
    {
        // Act
        var sut = PakaiBedModel.Create(
            _defaultReg,
            _defaultAudit,
            _defaultLayanan,
            _defaultBed,
            _defaultTipeKamar,
            _defaultKelas,
            150000m);

        // Assert
        sut.LayananDkId.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithExplicitLayananDkId_ShouldInitializeLayananDkId()
    {
        // Arrange
        var periode = new PeriodePakaiBedType(_defaultAudit, AuditInfoType.Default);
        var auditTrail = new AuditTrailType(_defaultAudit, AuditInfoType.Default, AuditInfoType.Default);

        // Act
        var sut = new PakaiBedModel(
            "PKB001",
            periode,
            _defaultReg.ToReff(),
            _defaultLayanan.ToReff(),
            _defaultBed.ToReff(),
            _defaultTipeKamar.ToReff(),
            _defaultKelas.ToReff(),
            150000m,
            auditTrail,
            layananDkId: "12");

        // Assert
        sut.LayananDkId.Should().Be("12");
    }
}
