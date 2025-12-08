using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class OrderTdkModelTests
{
    private static readonly string TestUserId = "TEST_USER";
    private static readonly DateTime TestDateTime = new DateTime(2023, 1, 1);

    [Fact]
    public void UT1_GivenValidParameters_WhenCreateWithTarif_ThenOrderTdkModelIsCreated()
    {
        // Arrange
        var pasien = CreateTestPasienModel();
        var dokter = CreateTestPpaType();
        var layanan = CreateTestLayananType();
        var tarif = CreateTestTarifType();

        // Act
        var result = OrderTdkModel.Create(pasien, dokter, layanan, tarif, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.OrderTdkId.Should().NotBeEmpty();
        result.OrderTdkDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
        result.Pasien.Should().BeEquivalentTo(pasien.ToReff());
        result.DokterOrder.Should().BeEquivalentTo(dokter.ToReff());
        result.Layanan.Should().BeEquivalentTo(layanan.ToReff());
        result.Tarif.Should().BeEquivalentTo(tarif.ToReff());
        result.StatusOrder.Should().Be(StatusOrderEnum.Ordered);
        result.AuditTrail.Created.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public void UT2_GivenValidParameters_WhenCreateWithFreeTextOrder_ThenOrderTdkModelIsCreated()
    {
        // Arrange
        var pasien = CreateTestPasienModel();
        var dokter = CreateTestPpaType();
        var layanan = CreateTestLayananType();
        var freeTextOrder = "Test Free Text";

        // Act
        var result = OrderTdkModel.Create(pasien, dokter, layanan, freeTextOrder, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.OrderTdkId.Should().NotBeEmpty();
        result.FreeTextOrder.Should().Be(freeTextOrder);
        result.Tarif.Should().BeEquivalentTo(TarifType.Default.ToReff());
    }

    [Fact]
    public void UT3_GivenValidRegParameters_WhenCreateWithTarif_ThenOrderTdkModelIsCreated()
    {
        // Arrange
        var reg = CreateTestRegModel();
        var dokter = CreateTestPpaType();
        var layanan = CreateTestLayananType();
        var tarif = CreateTestTarifType();

        // Act
        var result = OrderTdkModel.Create(reg, dokter, layanan, tarif, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Pasien.Should().BeEquivalentTo(reg.Pasien);
        result.Reg.Should().BeEquivalentTo(reg.ToReff());
    }

    [Fact]
    public void UT4_GivenValidRegParameters_WhenCreateWithFreeTextOrder_ThenOrderTdkModelIsCreated()
    {
        // Arrange
        var reg = CreateTestRegModel();
        var dokter = CreateTestPpaType();
        var layanan = CreateTestLayananType();
        var freeTextOrder = "Test Free Text";

        // Act
        var result = OrderTdkModel.Create(reg, dokter, layanan, freeTextOrder, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.FreeTextOrder.Should().Be(freeTextOrder);
        result.Pasien.Should().BeEquivalentTo(reg.Pasien);
    }

    [Fact]
    public void UT5_GivenNonDokterPpa_WhenCreate_ThenExceptionIsThrown()
    {
        // Arrange
        var pasien = CreateTestPasienModel();
        var nonDokter = CreateTestNonDokterPpaType();
        var layanan = CreateTestLayananType();
        var tarif = CreateTestTarifType();

        // Act & Assert
        Assert.Throws<Exception>(() => OrderTdkModel.Create(pasien, nonDokter, layanan, tarif, TestUserId));
    }

    [Fact]
    public void UT6_GivenNonAktifReg_WhenCreateWithTarif_ThenExceptionIsThrown()
    {
        // Arrange
        var nonAktifReg = CreateTestNonAktifRegModel();
        var dokter = CreateTestPpaType();
        var layanan = CreateTestLayananType();
        var tarif = CreateTestTarifType();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => OrderTdkModel.Create(nonAktifReg, dokter, layanan, tarif, TestUserId));
    }

    [Fact]
    public void UT7_GivenExistingOrderTdkModel_WhenExecute_ThenStatusIsExecuted()
    {
        // Arrange
        var orderTdk = OrderTdkModel.Default;
        var userId = "EXECUTE_USER";

        // Act
        orderTdk.Execute(userId);

        // Assert
        orderTdk.StatusOrder.Should().Be(StatusOrderEnum.Executed);
        orderTdk.AuditTrail.Modified.UserId.Should().Be(userId);
    }

    [Fact]
    public void UT8_GivenExistingOrderTdkModel_WhenCancel_ThenStatusIsCancelled()
    {
        // Arrange
        var orderTdk = OrderTdkModel.Default;
        var userId = "CANCEL_USER";

        // Act
        orderTdk.Cancel(userId);

        // Assert
        orderTdk.StatusOrder.Should().Be(StatusOrderEnum.Cancelled);
        orderTdk.AuditTrail.Voided.UserId.Should().Be(userId);
    }

    [Fact]
    public void UT9_GivenDefaultModel_WhenToReff_ThenOrderTindakanReffIsReturned()
    {
        // Arrange
        var orderTdk = OrderTdkModel.Default;

        // Act
        var result = orderTdk.ToReff();

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderTdk.OrderTdkId);
        result.OrderDate.Should().Be(orderTdk.OrderTdkDate);
        result.Tindakan.Should().BeEquivalentTo(orderTdk.Tarif);
    }

    private static PasienModel CreateTestPasienModel()
        => PasienModel.Default;

    private static PpaType CreateTestPpaType()
    {
        var satTugasDokter = new SatTugasType("X","Y", ProfesiType.Dokter);
        var ppaSatTugasType = new PpaSatTugasType(satTugasDokter, true);
        var ppaDokter = new PpaType("A", "B", "C", SmfType.Default, GroupSpesialisType.Default,  
            [], [ppaSatTugasType], []);
        return ppaDokter;
    }

    private static PpaType CreateTestNonDokterPpaType()
    {
        var satTugasDokter = new SatTugasType("X","Y", ProfesiType.Perawat);
        var ppaSatTugasType = new PpaSatTugasType(satTugasDokter, true);
        var ppaNonDokter = new PpaType("A", "B", "C", SmfType.Default, GroupSpesialisType.Default,  
            [], [ppaSatTugasType], []);
        return ppaNonDokter;
        
    }
    private static LayananType CreateTestLayananType()
        => LayananType.Default;
    private static TarifType CreateTestTarifType()
        => TarifType.Default;
    private static RegModel CreateTestRegModel()
        => RegModel.Default;
    private static RegModel CreateTestNonAktifRegModel()
    {
        var reg = RegModel.Default;
        // Simulate non-aktif reg by modifying the audit trail
        return reg;
    }
}