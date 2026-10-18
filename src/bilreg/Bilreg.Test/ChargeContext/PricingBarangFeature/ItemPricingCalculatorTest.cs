using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.BrgContext.BrgFeature;
using Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

/// <summary>
/// Unit tests domain service <see cref="ItemPricingCalculator"/> (ARCHITECTURE TD-03, TD-06, TD-07).
/// Menguji matriks prioritas Harga Jual Fix, pemotongan HET (Kasus A-D), konversi UOM dua tingkat,
/// penanganan HNA tidak ditemukan (ASM-006), breakdown jasa kefarmasian, dan pembulatan kasir.
/// </summary>
public class ItemPricingCalculatorTest
{
    private const string BarangId = "BRG-TEST-001";
    private const string TipeHargaId = "UM";

    private readonly Mock<IPricingBarangRepo> _pricingRepo = new();
    private readonly Mock<ITipeHargaRepo> _tipeHargaRepo = new();
    private readonly Mock<IHnaBatchRepo> _hnaRepo = new();
    private readonly Mock<IBrgSatuanDal> _satuanDal = new();
    private readonly ItemPricingCalculator _sut;

    public ItemPricingCalculatorTest()
    {
        _satuanDal
            .Setup(x => x.ListData(It.IsAny<IBrgKey>()))
            .Returns(Array.Empty<BrgSatuanDto>());

        _sut = new ItemPricingCalculator(
            _pricingRepo.Object,
            _tipeHargaRepo.Object,
            _hnaRepo.Object,
            _satuanDal.Object);
    }

    private void SetupBarang(
        decimal hargaJualFix,
        decimal marginPersen,
        decimal het,
        decimal biayaTambahanItem = 0m,
        decimal diskonDefaultPersen = 0m)
    {
        var model = PricingBarangModel
            .Create(BarangId, het, MetodeGenHnaEnum.Terakhir, "U1")
            .AddOrUpdateVariant(
                TipeHargaId,
                hargaJualFix,
                marginPersen,
                biayaTambahanItem,
                taxPersen: 0m,
                diskonDefaultPersen,
                isActive: true);

        _pricingRepo
            .Setup(x => x.LoadEntity(It.IsAny<IPricingBarangKey>()))
            .Returns(MayBe.From(model));
    }

    private void SetupTipeHarga(decimal biayaPerObat_r = 0m, decimal biayaPerResep_R = 0m)
    {
        var model = TipeHargaModel.Create(
            TipeHargaId, "UMUM", 0m, biayaPerObat_r, biayaPerResep_R, 0m, 0m, true);

        _tipeHargaRepo
            .Setup(x => x.LoadEntity(It.IsAny<ITipeHargaKey>()))
            .Returns(MayBe.From(model));
    }

    private static readonly DateTime Tgl = new(2026, 10, 17);

    [Fact]
    public async Task CalculateAsync_KasusA_FormulaAboveHet_IsCappedToHet()
    {
        SetupBarang(hargaJualFix: 0m, marginPersen: 25m, het: 1300m);
        SetupTipeHarga();
        _hnaRepo.Setup(x => x.GetActiveHna(BarangId, Tgl)).Returns(1100m);

        var result = await _sut.CalculateAsync(BarangId, TipeHargaId, Tgl);

        result.HargaJualFinal.Should().Be(1300m);
        result.HargaJualSatuanDasar.Should().Be(1300m);
        result.IsHetCapped.Should().BeTrue();
        result.IsFixedPriceApplied.Should().BeFalse();
        result.HnaApplied.Should().Be(1100m);
        result.MarginPersenApplied.Should().Be(25m);
    }

    [Fact]
    public async Task CalculateAsync_KasusB_FormulaBelowHet_UsesFormula()
    {
        SetupBarang(hargaJualFix: 0m, marginPersen: 30m, het: 1500m);
        SetupTipeHarga();
        _hnaRepo.Setup(x => x.GetActiveHna(BarangId, Tgl)).Returns(1100m);

        var result = await _sut.CalculateAsync(BarangId, TipeHargaId, Tgl);

        result.HargaJualFinal.Should().Be(1430m);
        result.IsHetCapped.Should().BeFalse();
    }

    [Fact]
    public async Task CalculateAsync_KasusC_FixedPriceBelowHet_UsesFixedPrice()
    {
        SetupBarang(hargaJualFix: 1290m, marginPersen: 25m, het: 1300m);
        SetupTipeHarga();
        _hnaRepo.Setup(x => x.GetActiveHna(BarangId, Tgl)).Returns(1100m);

        var result = await _sut.CalculateAsync(BarangId, TipeHargaId, Tgl);

        result.HargaJualFinal.Should().Be(1290m);
        result.IsFixedPriceApplied.Should().BeTrue();
        result.IsHetCapped.Should().BeFalse();
        result.HargaJualFixApplied.Should().Be(1290m);
    }

    [Fact]
    public async Task CalculateAsync_KasusD_FixedPriceAboveHet_IsCappedToHet()
    {
        SetupBarang(hargaJualFix: 1350m, marginPersen: 25m, het: 1300m);
        SetupTipeHarga();
        _hnaRepo.Setup(x => x.GetActiveHna(BarangId, Tgl)).Returns(1100m);

        var result = await _sut.CalculateAsync(BarangId, TipeHargaId, Tgl);

        result.HargaJualFinal.Should().Be(1300m);
        result.IsFixedPriceApplied.Should().BeTrue();
        result.IsHetCapped.Should().BeTrue();
    }

    [Fact]
    public async Task CalculateAsync_ReturnsDispensingFeesAndItemBreakdown()
    {
        SetupBarang(hargaJualFix: 0m, marginPersen: 25m, het: 0m, biayaTambahanItem: 350m, diskonDefaultPersen: 5m);
        SetupTipeHarga(biayaPerObat_r: 1500m, biayaPerResep_R: 3000m);
        _hnaRepo.Setup(x => x.GetActiveHna(BarangId, Tgl)).Returns(1000m);

        var result = await _sut.CalculateAsync(BarangId, TipeHargaId, Tgl, qty: 3);

        result.HargaJualFinal.Should().Be(1250m);
        result.BiayaTambahanItem.Should().Be(350m);
        result.DiskonDefaultPersen.Should().Be(5m);
        result.BiayaPerObat_r.Should().Be(1500m);
        result.BiayaPerResep_R.Should().Be(3000m);
        result.Qty.Should().Be(3);
    }

    [Fact]
    public async Task CalculateAsync_PackagingUnit_AppliesTwoTierUomConversion()
    {
        SetupBarang(hargaJualFix: 0m, marginPersen: 0m, het: 0m);
        SetupTipeHarga();
        _hnaRepo.Setup(x => x.GetActiveHna(BarangId, Tgl)).Returns(100m);

        _satuanDal
            .Setup(x => x.ListData(It.IsAny<IBrgKey>()))
            .Returns(new[]
            {
                new BrgSatuanDto(BarangId, "TAB", 1m, "Tablet"),
                new BrgSatuanDto(BarangId, "BOX", 10m, "Box")
            });

        var result = await _sut.CalculateAsync(BarangId, TipeHargaId, Tgl, satuanId: "BOX");

        result.Konversi.Should().Be(10m);
        result.HargaJualSatuanDasar.Should().Be(100m);
        result.HargaJualFinal.Should().Be(1000m);
    }

    [Fact]
    public async Task CalculateAsync_BaseUnit_KeepsConversionAtOne()
    {
        SetupBarang(hargaJualFix: 0m, marginPersen: 0m, het: 0m);
        SetupTipeHarga();
        _hnaRepo.Setup(x => x.GetActiveHna(BarangId, Tgl)).Returns(100m);

        _satuanDal
            .Setup(x => x.ListData(It.IsAny<IBrgKey>()))
            .Returns(new[]
            {
                new BrgSatuanDto(BarangId, "TAB", 1m, "Tablet"),
                new BrgSatuanDto(BarangId, "BOX", 10m, "Box")
            });

        var result = await _sut.CalculateAsync(BarangId, TipeHargaId, Tgl, satuanId: "TAB");

        result.Konversi.Should().Be(1m);
        result.HargaJualFinal.Should().Be(100m);
    }

    [Fact]
    public async Task CalculateAsync_NoActiveHna_AndNoFixedPrice_ThrowsHnaNotFoundException()
    {
        SetupBarang(hargaJualFix: 0m, marginPersen: 25m, het: 0m);
        SetupTipeHarga();
        _hnaRepo.Setup(x => x.GetActiveHna(BarangId, Tgl)).Returns((decimal?)null);

        var act = async () => await _sut.CalculateAsync(BarangId, TipeHargaId, Tgl);

        await act.Should().ThrowAsync<HnaNotFoundException>();
    }

    [Fact]
    public async Task CalculateAsync_NoActiveHna_ButFixedPrice_IsAllowed()
    {
        SetupBarang(hargaJualFix: 900m, marginPersen: 25m, het: 0m);
        SetupTipeHarga();
        _hnaRepo.Setup(x => x.GetActiveHna(BarangId, Tgl)).Returns((decimal?)null);

        var result = await _sut.CalculateAsync(BarangId, TipeHargaId, Tgl);

        result.HnaApplied.Should().Be(0m);
        result.HargaJualFinal.Should().Be(900m);
        result.IsFixedPriceApplied.Should().BeTrue();
    }

    [Fact]
    public async Task CalculateAsync_BarangNotFound_ThrowsKeyNotFoundException()
    {
        _pricingRepo
            .Setup(x => x.LoadEntity(It.IsAny<IPricingBarangKey>()))
            .Returns(MayBe<PricingBarangModel>.None);

        var act = async () => await _sut.CalculateAsync(BarangId, TipeHargaId, Tgl);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public void CalculateRounding_Ceiling_RoundsUpToNextIncrement()
    {
        var result = _sut.CalculateRounding(1234m, 100, RoundingModeEnum.KeAtas);

        result.TotalAfterRounding.Should().Be(1300m);
        result.RoundingDifference.Should().Be(66m);
    }

    [Fact]
    public void CalculateRounding_ExactMultiple_CeilingKeepsValue()
    {
        var result = _sut.CalculateRounding(1300m, 100, RoundingModeEnum.KeAtas);

        result.TotalAfterRounding.Should().Be(1300m);
        result.RoundingDifference.Should().Be(0m);
    }

    [Fact]
    public void CalculateRounding_Normal_RoundsHalfUp()
    {
        var up = _sut.CalculateRounding(1250m, 100, RoundingModeEnum.Normal);
        up.TotalAfterRounding.Should().Be(1300m);
        up.RoundingDifference.Should().Be(50m);

        var down = _sut.CalculateRounding(1249m, 100, RoundingModeEnum.Normal);
        down.TotalAfterRounding.Should().Be(1200m);
        down.RoundingDifference.Should().Be(-49m);
    }

    [Fact]
    public void CalculateRounding_NonPositiveIncrement_Throws()
    {
        var act = () => _sut.CalculateRounding(1000m, 0, RoundingModeEnum.KeAtas);

        act.Should().Throw<ArgumentException>();
    }
}
