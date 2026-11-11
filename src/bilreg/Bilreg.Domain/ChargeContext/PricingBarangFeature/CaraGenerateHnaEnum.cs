namespace Bilreg.Domain.ChargeContext.PricingBarangFeature;

/// <summary>
/// Cara Generate HNA: aturan komponen biaya faktur pengadaan yang disertakan dalam pembentukan
/// nilai dasar HNA sebelum diagregasi oleh <see cref="MetodeGenHnaEnum"/>.
/// </summary>
public enum CaraGenerateHnaEnum
{
    /// <summary>
    /// HPP (Harga Beli Murni): hanya harga beli dasar satuan netto sebelum pajak dan diskon.
    /// </summary>
    Hpp = 0,

    /// <summary>
    /// HPP + TAX: harga beli ditambah pengenaan Pajak Masukan (PPN) pengadaan.
    /// </summary>
    HppPlusTax = 1,

    /// <summary>
    /// HPP + TAX - DISKON: harga beli ditambah Pajak Masukan dikurangi diskon komersial distributor.
    /// </summary>
    HppPlusTaxMinDisc = 2
}
