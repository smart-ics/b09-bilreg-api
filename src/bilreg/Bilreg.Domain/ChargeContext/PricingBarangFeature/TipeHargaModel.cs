using Ardalis.GuardClauses;

namespace Bilreg.Domain.ChargeContext.PricingBarangFeature;

/// <summary>
/// Aggregate master klasifikasi tipe harga penjamin (tabel legacy <c>tb_tipe_barang</c>).
/// Memuat pemisahan jasa kefarmasian: Tuslah per-obat (<c>/r</c>) dan Embalase per-resep (<c>/R</c>).
/// </summary>
public record TipeHargaModel : ITipeHargaKey
{
    #region CREATION
    public TipeHargaModel(
        string tipeHargaId,
        string tipeHargaName,
        decimal defaultMarginPersen,
        decimal biayaPerObat_r,
        decimal biayaPerResep_R,
        decimal taxPersen,
        decimal diskonPersen,
        bool isActive)
    {
        TipeHargaId = tipeHargaId;
        TipeHargaName = tipeHargaName;
        DefaultMarginPersen = defaultMarginPersen;
        BiayaPerObat_r = biayaPerObat_r;
        BiayaPerResep_R = biayaPerResep_R;
        TaxPersen = taxPersen;
        DiskonPersen = diskonPersen;
        IsActive = isActive;
    }

    public static TipeHargaModel Create(
        string tipeHargaId,
        string tipeHargaName,
        decimal defaultMarginPersen,
        decimal biayaPerObat_r,
        decimal biayaPerResep_R,
        decimal taxPersen,
        decimal diskonPersen,
        bool isActive)
    {
        Guard.Against.NullOrWhiteSpace(tipeHargaId, nameof(tipeHargaId));
        Guard.Against.NullOrWhiteSpace(tipeHargaName, nameof(tipeHargaName));

        return new TipeHargaModel(
            tipeHargaId,
            tipeHargaName,
            defaultMarginPersen,
            biayaPerObat_r,
            biayaPerResep_R,
            taxPersen,
            diskonPersen,
            isActive);
    }

    public static TipeHargaModel Default => new(
        tipeHargaId: "-",
        tipeHargaName: "-",
        defaultMarginPersen: 0m,
        biayaPerObat_r: 0m,
        biayaPerResep_R: 0m,
        taxPersen: 0m,
        diskonPersen: 0m,
        isActive: false);

    public static ITipeHargaKey Key(string id) => Default with { TipeHargaId = id };
    #endregion

    #region PROPERTIES
    public string TipeHargaId { get; init; }
    public string TipeHargaName { get; init; }
    public decimal DefaultMarginPersen { get; init; }

    /// <summary>Biaya Tuslah per-obat (<c>/r</c>) dari kolom legacy <c>fn_biaya_per_barang</c>.</summary>
    public decimal BiayaPerObat_r { get; init; }

    /// <summary>Biaya Embalase per-resep (<c>/R</c>) dari kolom legacy <c>fn_biaya_per_racik</c>.</summary>
    public decimal BiayaPerResep_R { get; init; }

    public decimal TaxPersen { get; init; }
    public decimal DiskonPersen { get; init; }
    public bool IsActive { get; init; }
    #endregion
}
