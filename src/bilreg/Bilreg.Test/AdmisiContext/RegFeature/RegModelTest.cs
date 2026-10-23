using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegModelTest
{
    private static readonly DateOnly RegDate = new(2026, 6, 14);
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private static PpaReff DokterRef(string id, string name) => new(id, name);

    private static PpaType TestDokter(string id, string name)
    {
        var satTugas = new SatTugasType("ST1", "Dokter", ProfesiType.Dokter);
        return new PpaType(id, name, name, SmfType.Default, GroupSpesialisType.Default,
            listLayanan: [], listSatTugas: [new PpaSatTugasType(satTugas, true)], listContact: []);
    }

    private static RegModel CreateReg(
        PpaReff? dokter = null,
        AuditInfoType? regKeluarAudit = null,
        AuditInfoType? regVoidAudit = null,
        AuditInfoType? regCancelOutAudit = null)
    {
        var result = new RegModel(
            "RG00000001",
            RegDate,
            new AuditInfoType("tester", new DateTime(2026, 6, 14, 8, 0, 0)),
            regKeluarAudit ?? AuditInfoType.Default,
            regCancelOutAudit ?? AuditInfoType.Default,
            regVoidAudit ?? AuditInfoType.Default,
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
            RegEligibilityType.Default,
            []);
        return result;
    }

    [Fact]
    public void GivenActiveRegistration_WhenBatalBerobat_ThenUsesSuppliedTimestampAndBecomesInactive()
    {
        var registration = CreateReg();
        var timestamp = new DateTime(2026, 7, 12, 10, 30, 0);

        registration.BatalBerobat("void-user", timestamp);

        registration.RegVoidAudit.Should().Be(new AuditInfoType("void-user", timestamp));
        registration.RegKeluarAudit.Should().Be(AuditInfoType.Default);
        registration.RegCancelOutAudit.Should().Be(AuditInfoType.Default);
        registration.IsAktif.Should().BeFalse();
    }

    [Fact]
    public void GivenVoidedRegistration_WhenBatalBerobatAgain_ThenThrows()
    {
        var registration = CreateReg();
        registration.BatalBerobat("void-user", new DateTime(2026, 7, 12));

        var act = () => registration.BatalBerobat("void-user", new DateTime(2026, 7, 13));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dibatalkan*");
    }

    [Fact]
    public void GivenDischargedRegistration_WhenBatalBerobat_ThenThrows()
    {
        var registration = new RegModel(
            "RG00000002", RegDate,
            new AuditInfoType("tester", new DateTime(2026, 6, 14, 8, 0, 0)),
            new AuditInfoType("discharge-user", new DateTime(2026, 6, 15, 8, 0, 0)),
            AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegJalan, PasienModel.Default.ToReff(), TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(), KelasType.Default.ToReff(), CaraMasukDkType.Default,
            RujukanType.Default.ToReff(), PpaType.Default.ToReff(), LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(), RegEligibilityType.Default, []);

        var act = () => registration.BatalBerobat("void-user", new DateTime(2026, 7, 12));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah keluar*");
    }

    [Fact]
    public void GivenActiveRegistration_WhenCheckingIsAktif_ThenReturnsTrue()
    {
        // Arrange
        var registration = CreateReg();

        // Act & Assert
        registration.IsAktif.Should().BeTrue();
    }

    [Theory]
    [InlineData("   ", "   ")]
    [InlineData("", "")]
    [InlineData(null, null)]
    [InlineData("   ", "")]
    [InlineData("", "   ")]
    [InlineData("\t", " ")]
    public void GivenRegistrationWithWhitespaceAuditFields_WhenCheckingIsAktif_ThenReturnsTrue(string? keluarUser, string? voidUser)
    {
        // Arrange
        var keluarAudit = new AuditInfoType(keluarUser!, new DateTime(3000, 1, 1));
        var voidAudit = new AuditInfoType(voidUser!, new DateTime(3000, 1, 1));
        var registration = CreateReg(regKeluarAudit: keluarAudit, regVoidAudit: voidAudit);

        // Act & Assert
        registration.IsAktif.Should().BeTrue();
    }

    [Theory]
    [InlineData("   ", "3000-01-01", "00:00:00")]
    [InlineData("", "3000-01-01", "00:00:00")]
    [InlineData("   ", "9999-12-31", "23:59:59")]
    public void GivenRegistrationWithThreeArgConstructorWhitespaceAudits_WhenCheckingIsAktif_ThenReturnsTrue(string userId, string tgl, string jam)
    {
        // Arrange
        var keluarAudit = new AuditInfoType(userId, tgl, jam);
        var voidAudit = new AuditInfoType(userId, tgl, jam);
        var registration = CreateReg(regKeluarAudit: keluarAudit, regVoidAudit: voidAudit);

        // Act & Assert
        registration.IsAktif.Should().BeTrue();
    }

    [Fact]
    public void GivenRegistrationWithRealDischargeAudit_WhenCheckingIsAktif_ThenReturnsFalse()
    {
        // Arrange
        var dischargeAudit = new AuditInfoType("discharge-user", new DateTime(2026, 6, 15, 8, 0, 0));
        var registration = CreateReg(regKeluarAudit: dischargeAudit);

        // Act & Assert
        registration.IsAktif.Should().BeFalse();
    }

    [Fact]
    public void GivenRegistrationWithRealVoidAudit_WhenCheckingIsAktif_ThenReturnsFalse()
    {
        // Arrange
        var voidAudit = new AuditInfoType("void-user", new DateTime(2026, 6, 15, 8, 0, 0));
        var registration = CreateReg(regVoidAudit: voidAudit);

        // Act & Assert
        registration.IsAktif.Should().BeFalse();
    }

    [Fact]
    public void GivenRegistrationWithWhitespaceAudits_WhenBatalBerobat_ThenAllowsCancellationAndSetsVoidAudit()
    {
        // Arrange
        var keluarAudit = new AuditInfoType("   ", new DateTime(3000, 1, 1));
        var voidAudit = new AuditInfoType("   ", new DateTime(3000, 1, 1));
        var registration = CreateReg(regKeluarAudit: keluarAudit, regVoidAudit: voidAudit);
        var timestamp = new DateTime(2026, 7, 12, 10, 30, 0);

        // Act
        registration.BatalBerobat("void-user", timestamp);

        // Assert
        registration.RegVoidAudit.Should().Be(new AuditInfoType("void-user", timestamp));
        registration.IsAktif.Should().BeFalse();
    }

    [Fact]
    public void GivenVoidedRegistrationWithWhitespaceDischargeAudit_WhenBatalBerobat_ThenThrows()
    {
        // Arrange
        var keluarAudit = new AuditInfoType("   ", new DateTime(3000, 1, 1));
        var voidAudit = new AuditInfoType("void-user", new DateTime(2026, 6, 15, 8, 0, 0));
        var registration = CreateReg(regKeluarAudit: keluarAudit, regVoidAudit: voidAudit);

        // Act
        var act = () => registration.BatalBerobat("another-user", new DateTime(2026, 7, 12));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dibatalkan*");
    }

    [Fact]
    public void GivenDischargedRegistrationWithWhitespaceVoidAudit_WhenBatalBerobat_ThenThrows()
    {
        // Arrange
        var keluarAudit = new AuditInfoType("discharge-user", new DateTime(2026, 6, 15, 8, 0, 0));
        var voidAudit = new AuditInfoType("   ", new DateTime(3000, 1, 1));
        var registration = CreateReg(regKeluarAudit: keluarAudit, regVoidAudit: voidAudit);

        // Act
        var act = () => registration.BatalBerobat("void-user", new DateTime(2026, 7, 12));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah keluar*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GivenActiveRegistration_WhenBatalBerobatWithEmptyOrWhitespaceUserId_ThenThrowsArgumentException(string? invalidUserId)
    {
        // Arrange
        var registration = CreateReg();

        // Act
        var act = () => registration.BatalBerobat(invalidUserId!, DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*UserId wajib diisi*");
    }

    private static (LayananType Layanan, KarcisType Karcis) CreateRajalVisitData(string layananId = "LYN01")
    {
        var layanan = LayananType.Default with
        {
            LayananId = layananId,
            LayananName = "Poli Umum",
            InstalasiDk = InstalasiDkType.RawatJalan
        };
        var karcis = new KarcisType(
            "KRC01", "Karcis Test", true,
            InstalasiDkType.RawatJalan,
            RekapCetakType.Default.ToReff(),
            TarifType.Default.ToReff(),
            [new KarcisKomponenType(KomponenType.Default.ToReff(), 10000m)],
            [layanan.ToReff()]);
        return (layanan, karcis);
    }
}
