using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.BrgContext.PricingPolicyFeature;

public class BrgPricingServiceTest
{
    private readonly Mock<IBrgHargaJualDal> _dalMock;
    private readonly BrgPricingService _sut;

    public BrgPricingServiceTest()
    {
        _dalMock = new Mock<IBrgHargaJualDal>();
        _sut = new BrgPricingService(_dalMock.Object);
    }

    private static TipeBrgPricingConfig CreateDefaultRjConfig() =>
        new(
            TipeBrg: new TipeBrgReff("RJ", "RAWAT JALAN"),
            DefaultProfit: 0m,
            DefaultBiayaPerBarang: 0m,
            DefaultTax: 11m,
            DefaultDiskon: 0m);

    [Fact]
    public void CalculateItemPrice_ThrowsArgumentException_WhenBrgIdEmpty()
    {
        var act = () => _sut.CalculateItemPrice("", 1, "00000");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Barang ID cannot be empty*");
    }

    [Fact]
    public void CalculateItemPrice_ThrowsArgumentOutOfRangeException_WhenQtyNegative()
    {
        var act = () => _sut.CalculateItemPrice("BRG01", -1, "00000");
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Qty must be greater than or equal to zero*");
    }

    [Fact]
    public void CalculateItemPrice_ExplicitPrice_UsesHargaJual()
    {
        // Arrange
        const string brgId = "BRG01";
        const string tipeJaminanId = "00000";
        var tipeConfig = CreateDefaultRjConfig();

        _dalMock.Setup(x => x.ResolveTipeBrg(tipeJaminanId))
            .Returns(tipeConfig);

        var batchData = new Dictionary<string, BrgBatchPricingData>
        {
            [brgId] = new(
                BrgId: brgId,
                HasBarangTipe: true,
                HargaJual: 50000m,
                Profit: 20m,
                Biaya: 0m,
                Tax: 11m,
                Diskon: 0m,
                HnaGen: 30000m,
                HnaAkhir: 25000m,
                HppStok: 20000m)
        };

        _dalMock.Setup(x => x.GetBatchPricingData(It.IsAny<IEnumerable<string>>(), tipeConfig.TipeBrg.TipeBrgId, ""))
            .Returns(batchData);

        // Act
        var result = _sut.CalculateItemPrice(brgId, 1, tipeJaminanId);

        // Assert: Explicit price overrides HNA + profit
        result.Nilai.Harga.Should().Be(50000m);
        result.Nilai.SubTotal.Should().Be(50000m);
        result.Nilai.Tax.Should().Be(5500m);
        result.Nilai.Total.Should().Be(55500m);
    }

    [Fact]
    public void CalculateItemPrice_MarginCalculation_UsesHnaGenAndProfit()
    {
        // Arrange: fn_harga_jual == 0, fn_hna_gen > 0, fn_profit = 25%
        const string brgId = "BRG02";
        const string tipeJaminanId = "00000";
        var tipeConfig = CreateDefaultRjConfig();

        _dalMock.Setup(x => x.ResolveTipeBrg(tipeJaminanId))
            .Returns(tipeConfig);

        var batchData = new Dictionary<string, BrgBatchPricingData>
        {
            [brgId] = new(
                BrgId: brgId,
                HasBarangTipe: true,
                HargaJual: 0m,
                Profit: 25m,
                Biaya: 0m,
                Tax: 11m,
                Diskon: 0m,
                HnaGen: 10000m,
                HnaAkhir: 8000m,
                HppStok: 7000m)
        };

        _dalMock.Setup(x => x.GetBatchPricingData(It.IsAny<IEnumerable<string>>(), tipeConfig.TipeBrg.TipeBrgId, ""))
            .Returns(batchData);

        // Act
        var result = _sut.CalculateItemPrice(brgId, 1, tipeJaminanId);

        // Assert: Harga = Math.Round(10000 * (1 + 25/100), 2) = 12500
        result.Nilai.Harga.Should().Be(12500m);
    }

    [Fact]
    public void CalculateItemPrice_FallbackHnaAkhir_WhenHnaGenZero()
    {
        // Arrange: fn_hna_gen == 0, fallback to fn_hna_akhir
        const string brgId = "BRG03";
        const string tipeJaminanId = "00000";
        var tipeConfig = CreateDefaultRjConfig();

        _dalMock.Setup(x => x.ResolveTipeBrg(tipeJaminanId))
            .Returns(tipeConfig);

        var batchData = new Dictionary<string, BrgBatchPricingData>
        {
            [brgId] = new(
                BrgId: brgId,
                HasBarangTipe: true,
                HargaJual: 0m,
                Profit: 20m,
                Biaya: 0m,
                Tax: 11m,
                Diskon: 0m,
                HnaGen: 0m,
                HnaAkhir: 10000m,
                HppStok: 8000m)
        };

        _dalMock.Setup(x => x.GetBatchPricingData(It.IsAny<IEnumerable<string>>(), tipeConfig.TipeBrg.TipeBrgId, ""))
            .Returns(batchData);

        // Act
        var result = _sut.CalculateItemPrice(brgId, 1, tipeJaminanId);

        // Assert: Harga = Math.Round(10000 * (1 + 20/100), 2) = 12000
        result.Nilai.Harga.Should().Be(12000m);
    }

    [Fact]
    public void CalculateItemPrice_FallbackHppStok_WhenHnaZero()
    {
        // Arrange: HNA missing or 0, fallback to MAX(tb_stok.fn_hpp)
        const string brgId = "BRG04";
        const string tipeJaminanId = "00000";
        var tipeConfig = CreateDefaultRjConfig();

        _dalMock.Setup(x => x.ResolveTipeBrg(tipeJaminanId))
            .Returns(tipeConfig);

        var batchData = new Dictionary<string, BrgBatchPricingData>
        {
            [brgId] = new(
                BrgId: brgId,
                HasBarangTipe: true,
                HargaJual: 0m,
                Profit: 15m,
                Biaya: 0m,
                Tax: 11m,
                Diskon: 0m,
                HnaGen: 0m,
                HnaAkhir: 0m,
                HppStok: 5000m)
        };

        _dalMock.Setup(x => x.GetBatchPricingData(It.IsAny<IEnumerable<string>>(), tipeConfig.TipeBrg.TipeBrgId, ""))
            .Returns(batchData);

        // Act
        var result = _sut.CalculateItemPrice(brgId, 1, tipeJaminanId);

        // Assert: Harga = Math.Round(5000 * (1 + 15/100), 2) = 5750
        result.Nilai.Harga.Should().Be(5750m);
    }

    [Fact]
    public void CalculateItemPrice_TaxDiskonEmbalaseCalculation()
    {
        // Arrange: Qty = 2, Harga = 10000, Diskon = 10%, Embalase (Biaya) = 300, Tax = 11%
        const string brgId = "BRG05";
        const string tipeJaminanId = "00000";
        var tipeConfig = CreateDefaultRjConfig();

        _dalMock.Setup(x => x.ResolveTipeBrg(tipeJaminanId))
            .Returns(tipeConfig);

        var batchData = new Dictionary<string, BrgBatchPricingData>
        {
            [brgId] = new(
                BrgId: brgId,
                HasBarangTipe: true,
                HargaJual: 10000m,
                Profit: 0m,
                Biaya: 300m,
                Tax: 11m,
                Diskon: 10m,
                HnaGen: 8000m,
                HnaAkhir: 8000m,
                HppStok: 8000m)
        };

        _dalMock.Setup(x => x.GetBatchPricingData(It.IsAny<IEnumerable<string>>(), tipeConfig.TipeBrg.TipeBrgId, ""))
            .Returns(batchData);

        // Act: Qty = 2
        var result = _sut.CalculateItemPrice(brgId, 2, tipeJaminanId);

        // Assert:
        // Harga = 10000
        // Diskon = Math.Round((2 * 10000) * (10 / 100), 2) = 2000
        // Embalase = 300
        // SubTotal = (2 * 10000) - 2000 + 300 = 18300
        // Tax = Math.Round(18300 * (11 / 100), 0) = 2013
        // Total = 18300 + 2013 = 20313
        result.Nilai.Harga.Should().Be(10000m);
        result.Nilai.Diskon.Should().Be(2000m);
        result.Nilai.Embalase.Should().Be(300m);
        result.Nilai.SubTotal.Should().Be(18300m);
        result.Nilai.TaxProsen.Should().Be(11m);
        result.Nilai.Tax.Should().Be(2013m);
        result.Nilai.Total.Should().Be(20313m);
    }

    [Fact]
    public void CalculateItemPrice_FallbackTipeBrgDefaultRj_WhenTipeJaminanNotFoundOrEmpty()
    {
        // Arrange: tipeJaminanId is empty or unknown -> falls back to "RJ"
        const string brgId = "BRG06";
        var rjConfig = CreateDefaultRjConfig();

        _dalMock.Setup(x => x.ResolveTipeBrg(string.Empty))
            .Returns(rjConfig);

        var batchData = new Dictionary<string, BrgBatchPricingData>
        {
            [brgId] = new(
                BrgId: brgId,
                HasBarangTipe: false, // Not configured in TB_BARANG_TIPE, uses RJ defaults
                HargaJual: 0m,
                Profit: 0m,
                Biaya: 0m,
                Tax: 0m,
                Diskon: 0m,
                HnaGen: 1000m,
                HnaAkhir: 1000m,
                HppStok: 1000m)
        };

        _dalMock.Setup(x => x.GetBatchPricingData(It.IsAny<IEnumerable<string>>(), "RJ", ""))
            .Returns(batchData);

        // Act
        var result = _sut.CalculateItemPrice(brgId, 1, string.Empty);

        // Assert:
        result.TipeBrg.TipeBrgId.Should().Be("RJ");
        result.TipeBrg.TipeBrgName.Should().Be("RAWAT JALAN");
        result.Nilai.Harga.Should().Be(1000m);
        result.Nilai.TaxProsen.Should().Be(11m); // From DefaultTax of RJ
        result.Nilai.Tax.Should().Be(110m);
        result.Nilai.Total.Should().Be(1110m);
    }

    [Fact]
    public void GetUnitPrices_BatchPricing_ReturnsPricesWithoutNPlus1()
    {
        // Arrange
        var brgIds = new[] { "BRG01", "BRG02", "BRG03" };
        const string tipeJaminanId = "00000";
        var tipeConfig = CreateDefaultRjConfig();

        _dalMock.Setup(x => x.ResolveTipeBrg(tipeJaminanId))
            .Returns(tipeConfig);

        var batchData = new Dictionary<string, BrgBatchPricingData>
        {
            ["BRG01"] = new("BRG01", true, 50000m, 0, 0, 11, 0, 30000, 30000, 30000),
            ["BRG02"] = new("BRG02", true, 0m, 25, 0, 11, 0, 10000, 10000, 10000),
            ["BRG03"] = new("BRG03", false, 0m, 0, 0, 0, 0, 2000, 2000, 2000)
        };

        _dalMock.Setup(x => x.GetBatchPricingData(It.Is<IEnumerable<string>>(ids => ids.Count() == 3), "RJ", ""))
            .Returns(batchData);

        // Act
        var result = _sut.GetUnitPrices(brgIds, tipeJaminanId);

        // Assert
        result.Should().HaveCount(3);
        result["BRG01"].Should().Be(50000m); // Explicit price
        result["BRG02"].Should().Be(12500m); // 10000 * 1.25
        result["BRG03"].Should().Be(2000m);  // 2000 * 1.0 (default profit 0%)

        _dalMock.Verify(x => x.GetBatchPricingData(It.IsAny<IEnumerable<string>>(), "RJ", ""), Times.Once);
    }
}
