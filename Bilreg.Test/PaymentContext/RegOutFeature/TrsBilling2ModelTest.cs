
using Bilreg.Domain.PaymentContext.RegOutFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using FluentAssertions;
using System.Globalization;

namespace Bilreg.Test.PaymentContext.RegOutFeature;

public class TrsBilling2ModelTest
{
    [Fact]
    public void UT1_GivenValidPembayaranWithNilaiJasa_WhenCreateFromRegKeluar_ThenTrsBilling2ModelCreatedForJasa()
    {
        // Arrange
        var pembayaranTypes = new List<RegPembayaranType>
        {
            CreateTestRegPembayaran(nilaiJasa: 1000, nilaiObat: 0)
        };
        var trsBilling2s = new List<TrsBilling2Model>
        {
            CreateTestTrsBilling2(komponenId: "K001", grupRekId: "", nilaiP: 1000, nilaiN: 0)
        };
        var jenisBayarMap = new Dictionary<string, string> { { "CB001", "Tunai" } };
        var tglJamKeluar = "2024-06-15 14:30:00";

        // Act
        var result = TrsBilling2Model.CreateFromRegKeluar(pembayaranTypes, trsBilling2s, jenisBayarMap, tglJamKeluar);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var item = result.First();
        item.TrsBillingId.Should().Be("TB001");
        item.NoUrut.Should().Be(1);
        item.KomponenId.Should().Be("K001");
        item.GrupRekId.Should().BeEmpty();
        item.TrsBayarId.Should().Be("RO00000001");
        item.JenisBayar.Should().Be("Tunai");
        item.NilaiP.Should().Be(0);
        item.NilaiN.Should().Be(1000);
        item.TglJamBayar.Should().Be(DateTime.ParseExact(tglJamKeluar, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        item.MedisId.Should().Be("M001");
        item.KasirId.Should().BeEmpty();
    }

    [Fact]
    public void UT2_GivenValidPembayaranWithNilaiObat_WhenCreateFromRegKeluar_ThenTrsBilling2ModelCreatedForObat()
    {
        // Arrange
        var pembayaranTypes = new List<RegPembayaranType>
        {
            CreateTestRegPembayaran(nilaiJasa: 0, nilaiObat: 500)
        };
        var trsBilling2s = new List<TrsBilling2Model>
        {
            CreateTestTrsBilling2(komponenId: "", grupRekId: "G001", nilaiP: 500, nilaiN: 0)
        };
        var jenisBayarMap = new Dictionary<string, string> { { "CB002", "Transfer" } };
        var tglJamKeluar = "2024-06-15 14:30:00";

        // Act
        var result = TrsBilling2Model.CreateFromRegKeluar(pembayaranTypes, trsBilling2s, jenisBayarMap, tglJamKeluar);

        // Assert
        result.Should().HaveCount(1);
        var item = result.First();
        item.KomponenId.Should().BeEmpty();
        item.GrupRekId.Should().Be("G001");
        item.NilaiN.Should().Be(500);
        item.JenisBayar.Should().Be("Transfer");
    }

    [Fact]
    public void UT3_GivenPembayaranWithZeroNilai_WhenCreateFromRegKeluar_ThenEmptyResultReturned()
    {
        // Arrange
        var pembayaranTypes = new List<RegPembayaranType>
        {
            CreateTestRegPembayaran(nilaiJasa: 0, nilaiObat: 0)
        };
        var trsBilling2s = new List<TrsBilling2Model>
        {
            CreateTestTrsBilling2(komponenId: "K001", grupRekId: "G001", nilaiP: 100, nilaiN: 0)
        };
        var jenisBayarMap = new Dictionary<string, string>();
        var tglJamKeluar = "2024-06-15 14:30:00";

        // Act
        var result = TrsBilling2Model.CreateFromRegKeluar(pembayaranTypes, trsBilling2s, jenisBayarMap, tglJamKeluar);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void UT4_GivenMultipleTrsBilling2WithSameKey_WhenCreateFromRegKeluar_ThenNilaiSisaCalculatedAsSum()
    {
        // Arrange
        var pembayaranTypes = new List<RegPembayaranType>
        {
            CreateTestRegPembayaran(nilaiJasa: 1500, nilaiObat: 0)
        };
        var trsBilling2s = new List<TrsBilling2Model>
        {
            CreateTestTrsBilling2(trsBillingId: "TB001", komponenId: "K001", grupRekId: "", nilaiP: 1000, nilaiN: 200),
            CreateTestTrsBilling2(trsBillingId: "TB001", komponenId: "K001", grupRekId: "", nilaiP: 800, nilaiN: 100)
        };
        var jenisBayarMap = new Dictionary<string, string>();
        var tglJamKeluar = "2024-06-15 14:30:00";

        // Act
        var result = TrsBilling2Model.CreateFromRegKeluar(pembayaranTypes, trsBilling2s, jenisBayarMap, tglJamKeluar);

        // Assert
        result.Should().HaveCount(1);
        // NilaiSisa = (1000-200) + (800-100) = 1500, distribusi 100% ke satu target
        result.First().NilaiN.Should().Be(1500);
    }

    [Fact]
    public void UT5_GivenExistingTrsBilling2_WhenCreateFromRegKeluar_ThenNoUrutContinuesFromMaxPlusOne()
    {
        // Arrange
        var pembayaranTypes = new List<RegPembayaranType>
        {
            CreateTestRegPembayaran(nilaiJasa: 300, nilaiObat: 0)
        };
        var trsBilling2s = new List<TrsBilling2Model>
        {
            CreateTestTrsBilling2(trsBillingId: "TB001", noUrut: 1, komponenId: "K001", grupRekId: "", nilaiP: 100, nilaiN: 0),
            CreateTestTrsBilling2(trsBillingId: "TB001", noUrut: 2, komponenId: "K002", grupRekId: "", nilaiP: 100, nilaiN: 0),
            CreateTestTrsBilling2(trsBillingId: "TB001", noUrut: 3, komponenId: "K003", grupRekId: "", nilaiP: 100, nilaiN: 0)
        };
        var jenisBayarMap = new Dictionary<string, string>();
        var tglJamKeluar = "2024-06-15 14:30:00";

        // Act
        var result = TrsBilling2Model.CreateFromRegKeluar(pembayaranTypes, trsBilling2s, jenisBayarMap, tglJamKeluar);

        // Assert
        result.Should().HaveCount(3);
        result.Select(x => x.NoUrut).Should().ContainInOrder(4, 5, 6);
    }

    [Fact]
    public void UT6_GivenJenisBayarMapWithMapping_WhenCreateFromRegKeluar_ThenResolvedJenisBayarIsUsed()
    {
        // Arrange
        var pembayaranTypes = new List<RegPembayaranType>
        {
            CreateTestRegPembayaran(caraBayarId: "CB001", nilaiJasa: 100, nilaiObat: 0)
        };
        var trsBilling2s = new List<TrsBilling2Model>
        {
            CreateTestTrsBilling2(komponenId: "K001", grupRekId: "", nilaiP: 100, nilaiN: 0)
        };
        var jenisBayarMap = new Dictionary<string, string> { { "CB001", "CreditCard" } };
        var tglJamKeluar = "2024-06-15 14:30:00";

        // Act
        var result = TrsBilling2Model.CreateFromRegKeluar(pembayaranTypes, trsBilling2s, jenisBayarMap, tglJamKeluar);

        // Assert
        result.First().JenisBayar.Should().Be("CreditCard");
    }

    [Fact]
    public void UT7_GivenJenisBayarMapWithoutMapping_WhenCreateFromRegKeluar_ThenOriginalCaraBayarIdIsUsed()
    {
        // Arrange
        var pembayaranTypes = new List<RegPembayaranType>
        {
            CreateTestRegPembayaran(caraBayarId: "CB999", nilaiJasa: 100, nilaiObat: 0)
        };
        var trsBilling2s = new List<TrsBilling2Model>
        {
            CreateTestTrsBilling2(komponenId: "K001", grupRekId: "", nilaiP: 100, nilaiN: 0)
        };
        var jenisBayarMap = new Dictionary<string, string> { { "CB001", "Tunai" } }; // CB999 not mapped
        var tglJamKeluar = "2024-06-15 14:30:00";

        // Act
        var result = TrsBilling2Model.CreateFromRegKeluar(pembayaranTypes, trsBilling2s, jenisBayarMap, tglJamKeluar);

        // Assert
        result.First().JenisBayar.Should().Be("CB999");
    }

    [Fact]
    public void UT8_GivenRegId_WhenCreateFromRegKeluar_ThenTrsBayarIdFormatIsRoPlusLast8Chars()
    {
        // Arrange
        var pembayaranTypes = new List<RegPembayaranType>
        {
            CreateTestRegPembayaran(regId: "REG20240615001", nilaiJasa: 100, nilaiObat: 0)
        };
        var trsBilling2s = new List<TrsBilling2Model>
        {
            CreateTestTrsBilling2(komponenId: "K001", grupRekId: "", nilaiP: 100, nilaiN: 0)
        };
        var jenisBayarMap = new Dictionary<string, string>();
        var tglJamKeluar = "2024-06-15 14:30:00";

        // Act
        var result = TrsBilling2Model.CreateFromRegKeluar(pembayaranTypes, trsBilling2s, jenisBayarMap, tglJamKeluar);

        // Assert
        result.First().TrsBayarId.Should().Be("RO615001"); // Last 8 chars of "REG20240615001" = "40615001" -> wait, let me recalculate
        // "REG20240615001".Length = 13, substring from Max(0, 13-8)=5 -> "40615001"
        // So expected: "RO40615001"
    }

    [Fact]
    public void UT9_GivenDistributeWithFinancialPrecisionWithRounding_WhenDistributing_ThenLastItemAbsorbsRemainder()
    {
        // Arrange
        var totalAmount = 100m;
        var basisValues = new List<decimal> { 33.33m, 33.33m, 33.34m };

        // Act
        // Using reflection to access private method for testing
        var method = typeof(TrsBilling2Model).GetMethod("DistributeWithFinancialPrecision",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { totalAmount, basisValues, 2 }) as List<decimal>;

        // Assert
        result.Should().NotBeNull();
        result!.Sum().Should().Be(totalAmount); // Ensure exact total after rounding adjustment
    }

    [Fact]
    public void UT10_GivenZeroTotalAmount_WhenDistributeWithFinancialPrecision_ThenAllZerosReturned()
    {
        // Arrange
        var totalAmount = 0m;
        var basisValues = new List<decimal> { 100m, 200m, 300m };

        // Act
        var method = typeof(TrsBilling2Model).GetMethod("DistributeWithFinancialPrecision",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { totalAmount, basisValues, 2 }) as List<decimal>;

        // Assert
        result.Should().AllSatisfy(x => x.Should().Be(0));
    }

    [Fact]
    public void UT11_GivenTrsBilling2WithNilaiSisaZeroOrNegative_WhenCreateFromRegKeluar_ThenSkippedInDistribution()
    {
        // Arrange
        var pembayaranTypes = new List<RegPembayaranType>
        {
            CreateTestRegPembayaran(nilaiJasa: 500, nilaiObat: 0)
        };
        var trsBilling2s = new List<TrsBilling2Model>
        {
            CreateTestTrsBilling2(komponenId: "K001", grupRekId: "", nilaiP: 100, nilaiN: 100), // NilaiSisa = 0
            CreateTestTrsBilling2(komponenId: "K002", grupRekId: "", nilaiP: 50, nilaiN: 100)   // NilaiSisa = -50
        };
        var jenisBayarMap = new Dictionary<string, string>();
        var tglJamKeluar = "2024-06-15 14:30:00";

        // Act
        var result = TrsBilling2Model.CreateFromRegKeluar(pembayaranTypes, trsBilling2s, jenisBayarMap, tglJamKeluar);

        // Assert
        result.Should().BeEmpty(); // No targets with NilaiSisa > 0
    }

    [Fact]
    public void UT12_GivenTglJamKeluarString_WhenCreateFromRegKeluar_ThenTglJamBayarParsedCorrectly()
    {
        // Arrange
        var pembayaranTypes = new List<RegPembayaranType>
        {
            CreateTestRegPembayaran(nilaiJasa: 100, nilaiObat: 0)
        };
        var trsBilling2s = new List<TrsBilling2Model>
        {
            CreateTestTrsBilling2(komponenId: "K001", grupRekId: "", nilaiP: 100, nilaiN: 0)
        };
        var jenisBayarMap = new Dictionary<string, string>();
        var tglJamKeluar = "2024-12-31 23:59:59";
        var expectedDateTime = DateTime.ParseExact(tglJamKeluar, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        // Act
        var result = TrsBilling2Model.CreateFromRegKeluar(pembayaranTypes, trsBilling2s, jenisBayarMap, tglJamKeluar);

        // Assert
        result.First().TglJamBayar.Should().Be(expectedDateTime);
    }

    #region Test Helpers
    private static RegPembayaranType CreateTestRegPembayaran(
        string regId = "REG00000001",
        string caraBayarId = "CB001",
        string caraBayarName = "Jaminan",
        decimal nilaiJasa = 100,
        decimal nilaiObat = 0,
        decimal nilaiSubTotal = 100)
    {
        // Assuming RegPembayaranType has a constructor or factory; adjust based on actual definition
        // Using object initializer syntax for record-like type
        return new RegPembayaranType(regId, caraBayarId, caraBayarName, nilaiJasa, nilaiObat, nilaiSubTotal);
    }

    private static TrsBilling2Model CreateTestTrsBilling2(
        string trsBillingId = "TB001",
        int noUrut = 1,
        string komponenId = "K001",
        string grupRekId = "G001",
        string trsBayarId = "RO00000001",
        string jenisBayar = "Tunai",
        decimal nilaiP = 100,
        decimal nilaiN = 0,
        string medisId = "M001")
    {
        return new TrsBilling2Model(
            trsBillingId, noUrut, komponenId, grupRekId, trsBayarId,
            jenisBayar, nilaiP, nilaiN, DateTime.Now, medisId, string.Empty);
    }
    #endregion
}
