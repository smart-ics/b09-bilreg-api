using Ardalis.GuardClauses;

namespace Bilreg.Domain.ChargeContext.PricingBarangFeature;

/// <summary>
/// Child value object yang menyimpan varian harga jual per tipe penjamin
/// (baris tabel legacy <c>tb_barang_tipe</c>): Override Harga Jual Fix, margin persentase,
/// biaya penanganan item, pajak, dan diskon default.
/// </summary>
public record ItemPricingVariantType : IItemPricingVariantKey
{
    #region CREATION

    public ItemPricingVariantType(
        string tipeHargaId,
        decimal hargaJualFix,
        decimal marginPersen,
        decimal biayaTambahanItem,
        decimal taxPersen,
        decimal diskonDefaultPersen,
        bool isActive)
    {
        Guard.Against.NullOrWhiteSpace(tipeHargaId, nameof(tipeHargaId));
        Guard.Against.Negative(hargaJualFix, nameof(hargaJualFix));
        Guard.Against.Negative(marginPersen, nameof(marginPersen));
        Guard.Against.Negative(biayaTambahanItem, nameof(biayaTambahanItem));
        Guard.Against.Negative(taxPersen, nameof(taxPersen));
        Guard.Against.Negative(diskonDefaultPersen, nameof(diskonDefaultPersen));

        TipeHargaId = tipeHargaId;
        HargaJualFix = hargaJualFix;
        MarginPersen = marginPersen;
        BiayaTambahanItem = biayaTambahanItem;
        TaxPersen = taxPersen;
        DiskonDefaultPersen = diskonDefaultPersen;
        IsActive = isActive;
    }

    public static ItemPricingVariantType Create(
        string tipeHargaId,
        decimal hargaJualFix,
        decimal marginPersen,
        decimal biayaTambahanItem,
        decimal taxPersen,
        decimal diskonDefaultPersen,
        bool isActive) =>
        new(tipeHargaId, hargaJualFix, marginPersen, biayaTambahanItem, taxPersen, diskonDefaultPersen, isActive);

    public static ItemPricingVariantType Default => new(
        tipeHargaId: "-",
        hargaJualFix: 0m,
        marginPersen: 0m,
        biayaTambahanItem: 0m,
        taxPersen: 0m,
        diskonDefaultPersen: 0m,
        isActive: false);

    public static IItemPricingVariantKey Key(string tipeHargaId) =>
        Default with { TipeHargaId = tipeHargaId };

    #endregion

    #region PROPERTIES

    /// <summary>Kode tipe harga / tipe penjamin (mis. <c>UM</c>, <c>JM</c>, <c>KW</c>).</summary>
    public string TipeHargaId { get; init; }

    /// <summary>Override Harga Jual Fix (kolom legacy <c>fn_harga_jual</c>). Aktif bila <c>&gt; 0</c>.</summary>
    public decimal HargaJualFix { get; init; }

    /// <summary>Persentase margin keuntungan tipe penjamin (kolom legacy <c>fn_profit</c>).</summary>
    public decimal MarginPersen { get; init; }

    /// <summary>Biaya penanganan khusus item (kolom legacy <c>fn_biaya</c>).</summary>
    public decimal BiayaTambahanItem { get; init; }

    /// <summary>Persentase pajak penjualan (kolom legacy <c>fn_tax</c>).</summary>
    public decimal TaxPersen { get; init; }

    /// <summary>Diskon default item kepada pasien (kolom legacy <c>fn_diskon</c>).</summary>
    public decimal DiskonDefaultPersen { get; init; }

    /// <summary>Status keaktifan tarif untuk tipe penjamin ini (kolom legacy <c>fb_jual</c>).</summary>
    public bool IsActive { get; init; }

    #endregion

    #region BEHAVIOUR

    /// <summary>
    /// Menandakan Override Harga Jual Fix aktif secara mutlak ketika <c>HargaJualFix &gt; 0</c>,
    /// sehingga kalkulasi HNA dan margin diabaikan sepenuhnya (TD-03 Prioritas 1).
    /// </summary>
    public bool IsFixedPriceOverride => HargaJualFix > 0m;

    /// <summary>
    /// Menghasilkan harga jual satuan terpilih <b>sebelum</b> pemotongan plafon HET (TD-03 langkah 1-2):
    /// Override Harga Jual Fix bila aktif, jika tidak maka <c>HNA x (1 + MarginPersen / 100)</c>.
    /// </summary>
    /// <param name="hna">Harga Netto Apotek aktif per satuan terkecil.</param>
    public decimal CalculateUnitPrice(decimal hna)
    {
        Guard.Against.Negative(hna, nameof(hna));

        return IsFixedPriceOverride
            ? HargaJualFix
            : hna * (1m + MarginPersen / 100m);
    }

    #endregion
}

public interface IItemPricingVariantKey
{
    string TipeHargaId { get; }
}
