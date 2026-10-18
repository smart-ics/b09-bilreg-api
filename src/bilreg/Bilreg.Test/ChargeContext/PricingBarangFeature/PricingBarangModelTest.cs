using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using FluentAssertions;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

public class PricingBarangModelTest
{
    private static readonly DateTime TestCreatedAt = new(2026, 10, 17, 8, 0, 0);

    private static PricingBarangModel BuildModel(
        decimal hetNominal = 0m,
        MetodeGenHnaEnum metodeGenHna = MetodeGenHnaEnum.Terakhir) =>
        PricingBarangModel.Create("BRG0001", hetNominal, metodeGenHna, "U1", TestCreatedAt);

    private static PricingBarangModel WithVariant(
        decimal hetNominal,
        decimal hargaJualFix,
        decimal marginPersen,
        string tipeHargaId = "UM") =>
        BuildModel(hetNominal).AddOrUpdateVariant(
            tipeHargaId, hargaJualFix, marginPersen, 0m, 0m, 0m, true);

    #region KASUS A-D (Outcome)

    // Kasus A: HNA 1.100, Margin 25% (1.375), tanpa HJF, HET 1.300 -> dipangkas ke HET.
    [Fact]
    public void CalculatePrice_KasusA_FormulaExceedsHet_IsCappedExactlyToHet()
    {
        var model = WithVariant(hetNominal: 1300m, hargaJualFix: 0m, marginPersen: 25m);

        var result = model.CalculatePrice("UM", 1100m);

        result.HargaJualTerpilih.Should().Be(1375m);
        result.HargaJualFinal.Should().Be(1300m);
        result.IsHetCapped.Should().BeTrue();
    }

    // Kasus B: HNA 1.100, Margin 30% (1.430), tanpa HJF, HET 1.500 -> formula normal.
    [Fact]
    public void CalculatePrice_KasusB_FormulaBelowHet_UsesFormula()
    {
        var model = WithVariant(hetNominal: 1500m, hargaJualFix: 0m, marginPersen: 30m);

        var result = model.CalculatePrice("UM", 1100m);

        result.HargaJualTerpilih.Should().Be(1430m);
        result.HargaJualFinal.Should().Be(1430m);
        result.IsHetCapped.Should().BeFalse();
    }

    // Kasus C: HNA 1.100, Margin 25% (1.375), HJF 1.290, HET 1.300 -> HJF mengalahkan formula, di bawah HET.
    [Fact]
    public void CalculatePrice_KasusC_FixedPriceOverridesFormula_NotCapped()
    {
        var model = WithVariant(hetNominal: 1300m, hargaJualFix: 1290m, marginPersen: 25m);

        var result = model.CalculatePrice("UM", 1100m);

        result.HargaJualTerpilih.Should().Be(1290m);
        result.HargaJualFinal.Should().Be(1290m);
        result.IsHetCapped.Should().BeFalse();
    }

    // Kasus D: HNA 1.100, Margin 25% (1.375), HJF 1.350, HET 1.300 -> HJF dipangkas ke HET.
    [Fact]
    public void CalculatePrice_KasusD_FixedPriceExceedsHet_IsCappedToHet()
    {
        var model = WithVariant(hetNominal: 1300m, hargaJualFix: 1350m, marginPersen: 25m);

        var result = model.CalculatePrice("UM", 1100m);

        result.HargaJualTerpilih.Should().Be(1350m);
        result.HargaJualFinal.Should().Be(1300m);
        result.IsHetCapped.Should().BeTrue();
    }

    #endregion

    #region CALCULATE PRICE INVARIANTS

    [Fact]
    public void CalculatePrice_FixedPriceOverride_IgnoresHnaAndMarginEntirely()
    {
        var model = WithVariant(hetNominal: 0m, hargaJualFix: 500m, marginPersen: 999m);

        var result = model.CalculatePrice("UM", 1_000_000m);

        result.HargaJualTerpilih.Should().Be(500m);
        result.HargaJualFinal.Should().Be(500m);
        result.IsHetCapped.Should().BeFalse();
    }

    [Fact]
    public void CalculatePrice_WhenHetIsZero_DoesNotCapEvenIfPriceIsHigh()
    {
        var model = WithVariant(hetNominal: 0m, hargaJualFix: 0m, marginPersen: 100m);

        var result = model.CalculatePrice("UM", 1000m);

        result.HargaJualTerpilih.Should().Be(2000m);
        result.HargaJualFinal.Should().Be(2000m);
        result.IsHetCapped.Should().BeFalse();
    }

    [Fact]
    public void CalculatePrice_WhenPriceEqualsHet_IsNotCapped()
    {
        var model = WithVariant(hetNominal: 1375m, hargaJualFix: 0m, marginPersen: 25m);

        var result = model.CalculatePrice("UM", 1100m);

        result.HargaJualFinal.Should().Be(1375m);
        result.IsHetCapped.Should().BeFalse();
    }

    [Fact]
    public void CalculatePrice_UnknownTipeHarga_Throws()
    {
        var model = WithVariant(hetNominal: 1000m, hargaJualFix: 0m, marginPersen: 25m);

        var act = () => model.CalculatePrice("JM", 1100m);

        act.Should().Throw<KeyNotFoundException>().WithMessage("*JM*tidak ditemukan*");
    }

    [Fact]
    public void CalculatePrice_WithNegativeHna_Throws()
    {
        var model = WithVariant(hetNominal: 1000m, hargaJualFix: 0m, marginPersen: 25m);

        var act = () => model.CalculatePrice("UM", -1m);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region ADD OR UPDATE VARIANT

    [Fact]
    public void AddOrUpdateVariant_WhenTipeHargaExists_ReplacesVariant()
    {
        var model = WithVariant(hetNominal: 0m, hargaJualFix: 0m, marginPersen: 25m);

        var updated = model.AddOrUpdateVariant("UM", 0m, 40m, 500m, 11m, 5m, true);

        updated.Variants.Should().HaveCount(1);
        var variant = updated.Variants.Single();
        variant.TipeHargaId.Should().Be("UM");
        variant.MarginPersen.Should().Be(40m);
        variant.BiayaTambahanItem.Should().Be(500m);
        variant.TaxPersen.Should().Be(11m);
        variant.DiskonDefaultPersen.Should().Be(5m);
        variant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AddOrUpdateVariant_ForDistinctTipeHarga_KeepsAllVariants()
    {
        var model = BuildModel()
            .AddOrUpdateVariant("UM", 0m, 25m, 0m, 0m, 0m, true)
            .AddOrUpdateVariant("JM", 0m, 20m, 0m, 0m, 0m, true)
            .AddOrUpdateVariant("KW", 0m, 10m, 0m, 0m, 0m, true);

        model.Variants.Should().HaveCount(3);
        model.CalculatePrice("JM", 1100m).HargaJualFinal.Should().Be(1320m);
        model.CalculatePrice("KW", 1100m).HargaJualFinal.Should().Be(1210m);
    }

    [Fact]
    public void AddOrUpdateVariant_PreservesHeaderProperties()
    {
        var model = BuildModel(hetNominal: 1300m, metodeGenHna: MetodeGenHnaEnum.RataRata);

        var updated = model.AddOrUpdateVariant("UM", 0m, 25m, 0m, 0m, 0m, true);

        updated.BarangId.Should().Be("BRG0001");
        updated.HetNominal.Should().Be(1300m);
        updated.MetodeGenHna.Should().Be(MetodeGenHnaEnum.RataRata);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, 0)]
    [InlineData(0, -1, 0, 0, 0)]
    [InlineData(0, 0, -1, 0, 0)]
    [InlineData(0, 0, 0, -1, 0)]
    [InlineData(0, 0, 0, 0, -1)]
    public void AddOrUpdateVariant_WithNegativeMonetaryValue_Throws(
        decimal hargaJualFix,
        decimal marginPersen,
        decimal biayaTambahanItem,
        decimal taxPersen,
        decimal diskonDefaultPersen)
    {
        var model = BuildModel();

        var act = () => model.AddOrUpdateVariant(
            "UM", hargaJualFix, marginPersen, biayaTambahanItem, taxPersen, diskonDefaultPersen, true);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region CREATION & INVARIANTS

    [Fact]
    public void Create_AssignsHeaderPropertiesAndAuditTrail()
    {
        var model = BuildModel(hetNominal: 1300m, metodeGenHna: MetodeGenHnaEnum.Terbesar);

        model.BarangId.Should().Be("BRG0001");
        model.HetNominal.Should().Be(1300m);
        model.MetodeGenHna.Should().Be(MetodeGenHnaEnum.Terbesar);
        model.Variants.Should().BeEmpty();
        model.AuditTrail.Created.UserId.Should().Be("U1");
        model.AuditTrail.IsVoided.Should().BeFalse();
    }

    [Fact]
    public void Create_WithNegativeHet_Throws()
    {
        var act = () => BuildModel(hetNominal: -1m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithBlankBarangId_Throws()
    {
        var act = () => PricingBarangModel.Create(
            " ", 0m, MetodeGenHnaEnum.Terakhir, "U1", TestCreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ItemPricingVariantType_Default_HasNoFixedPriceOverride()
    {
        var variant = ItemPricingVariantType.Default;

        variant.IsFixedPriceOverride.Should().BeFalse();
        variant.MarginPersen.Should().Be(0m);
        variant.CalculateUnitPrice(1000m).Should().Be(1000m);
    }

    #endregion
}
