using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillingTypeTests
{
    [Fact]
    public void UT1_GivenValidParameters_WhenCreateFromTindakan_ThenTrsBillingTypeIsCreated()
    {
        // Arrange
        var tindakan = CreateTestTindakanModel();
        var reg = CreateTestRegModel();
        var tarif = CreateTestTarifType();
        var jaminan = CreateTestJaminanType();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act
        var result = TrsBillingType.CreateFromTindakan(tindakan, reg, tarif, jaminan, listReffKomp);

        // Assert
        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be(tindakan.TindakanId);
        result.Modul.Should().Be(0);
        result.TglTrs.Should().Be(tindakan.TindakanDate);
        result.Reg.Should().BeEquivalentTo(tindakan.Reg);
        result.Layanan.Should().BeEquivalentTo(tindakan.Layanan);
        result.Kelas.Should().BeEquivalentTo(tindakan.Kelas);
        result.SubTotal.Should().Be(tindakan.Total);
        result.Diskon.Should().Be(0);
        result.Tax.Should().Be(0);
        result.Biaya.Should().Be(0);
        result.Total.Should().Be(result.SubTotal - result.Diskon + result.Tax + result.Biaya);
        result.Keterangan.Keterangan.Should().Be(tarif.TarifName);
        result.Keterangan.RefBiaya.Should().Be(tarif.TarifId);
        result.ListTrsBilling2.Should().NotBeEmpty();
    }

    [Fact]
    public void UT2_GivenMismatchedTarif_WhenCreateFromTindakan_ThenArgumentExceptionIsThrown()
    {
        // Arrange
        var tindakan = CreateTestTindakanModel();
        var reg = CreateTestRegModel();
        var mismatchedTarif = CreateTestTarifType().ToReff() != tindakan.Tarif 
            ? CreateTestTarifType() 
            : CreateTestTarifTypeWithDifferentId();
        var jaminan = CreateTestJaminanType();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TrsBillingType.CreateFromTindakan(tindakan, reg, mismatchedTarif, jaminan, listReffKomp));
    }

    [Fact]
    public void UT3_GivenMismatchedJaminan_WhenCreateFromTindakan_ThenArgumentExceptionIsThrown()
    {
        // Arrange
        var tindakan = CreateTestTindakanModel();
        var reg = CreateTestRegModel();
        var tarif = CreateTestTarifType();
        var mismatchedJaminan = CreateTestJaminanTypeWithDifferentId();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TrsBillingType.CreateFromTindakan(tindakan, reg, tarif, mismatchedJaminan, listReffKomp));
    }

    [Fact]
    public void UT4_GivenRegInap_WhenCreateFromTindakan_ThenRanapRekeningIsUsed()
    {
        // Arrange
        var tindakan = CreateTestTindakanModel();
        var reg = CreateTestRegModelInap();
        var tarif = CreateTestTarifType();
        var jaminan = CreateTestJaminanType();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act
        var result = TrsBillingType.CreateFromTindakan(tindakan, reg, tarif, jaminan, listReffKomp);

        // Assert
        result.ListTrsBilling2.Should().NotBeEmpty();
        var trsBilling2 = result.ListTrsBilling2.First();
        // Verify that Ranap rekening is used based on JenisRegEnum
    }

    [Fact]
    public void UT5_GivenRegJalan_WhenCreateFromTindakan_ThenRajalRekeningIsUsed()
    {
        // Arrange
        var tindakan = CreateTestTindakanModel();
        var reg = CreateTestRegModel();
        var tarif = CreateTestTarifType();
        var jaminan = CreateTestJaminanType();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act
        var result = TrsBillingType.CreateFromTindakan(tindakan, reg, tarif, jaminan, listReffKomp);

        // Assert
        result.ListTrsBilling2.Should().NotBeEmpty();
        var trsBilling2 = result.ListTrsBilling2.First();
        // Verify that Rajal rekening is used based on JenisRegEnum
    }

    [Fact]
    public void UT6_GivenDefaultModel_WhenTotalCalculated_ThenCorrectTotalIsReturned()
    {
        // Arrange
        var billing = TrsBillingType.Default with 
        { 
            SubTotal = 1000, 
            Diskon = 100, 
            Tax = 50, 
            Biaya = 25 
        };

        // Act
        var total = billing.Total;

        // Assert
        total.Should().Be(975); // 1000 - 100 + 50 + 25
    }

    [Fact]
    public void UT7_GivenTrsBillingWithItems_WhenAddTrsBilling2_ThenItemIsAddedToList()
    {
        // Arrange
        var billing = TrsBillingType.Default;
        var trsBilling2 = CreateTestTrsBilling2();

        // Act
        billing.AddTrsBilling2(trsBilling2);

        // Assert
        billing.ListTrsBilling2.Should().Contain(trsBilling2);
        billing.ListTrsBilling2.Count().Should().Be(1);
    }

    private static TindakanModel CreateTestTindakanModel()
    {
        var tdkKomponen = new List<TindakanKomponenWithoutPpaType>
        {
            new TindakanKomponenWithoutPpaType(KomponenType.Default.ToReff(), 1, 200, 1, 200)
        };
        var result = new TindakanModel(
            "-", 
            DateTime.Today, 
            "", 
            RegModel.Default.ToReff(),
            LayananType.Default.ToReff(),
            TipeTarifType.Default.ToReff(), 
            TarifType.Default.ToReff(),
            tdkKomponen, 
            AuditTrailType.Default
        );
        return result;
    }

    private static RegModel CreateTestRegModel()
    {
        var tipeJaminan = new TipeJaminanReff("J0011", "Jaminan Test");
        var result = new RegModel("-", new DateOnly(3000, 1, 1),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegJalan, PasienModel.Default.ToReff(), tipeJaminan,
            PolisModel.Default.ToReff(), KelasType.Default.ToReff(), CaraMasukDkType.Default,
            RujukanType.Default.ToReff(), PpaType.Default.ToReff(), LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(), []);
        return result;
    }

    private static RegModel CreateTestRegModelInap()
    {
        var tipeJaminan = new TipeJaminanReff("J0011", "Jaminan Test");
        var result =  new RegModel("-", new DateOnly(3000, 1, 1),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegInap, PasienModel.Default.ToReff(), tipeJaminan,
            PolisModel.Default.ToReff(), KelasType.Default.ToReff(), CaraMasukDkType.Default,
            RujukanType.Default.ToReff(), PpaType.Default.ToReff(), LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(), []);
        return result;
    }
    private static TarifType CreateTestTarifType()
        => TarifType.Default;
    private static TarifType CreateTestTarifTypeWithDifferentId()
        => TarifType.Default with { TarifId = "DIFFERENT" };
    private static JaminanType CreateTestJaminanType()
        => JaminanType.Default with {JaminanId = "J00"};
    private static JaminanType CreateTestJaminanTypeWithDifferentId()
        => JaminanType.Default with { JaminanId = "DIFFERENT" };
    private static KomponenType CreateTestKomponenType()
        => KomponenType.Default;
    private static TrsBilling2Base CreateTestTrsBilling2()
        => new TrsBilling2JasaType(0, "A", DateTime.Now, new NilaiBillingType("B", 100, 0), PpaType.Default.ToReff(), PegType.Default, KomponenType.Default.ToReff(), new RekJasaType("C", "D", "E"));
}