using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.ChargeContext.PricingBarangFeature;

/// <summary>
/// Child entity batch HNA per barang (tabel legacy <c>tb_trs_hna2</c>).
/// Memuat variasi HNA hasil komputasi pengadaan serta aturan temporal <i>effective-dating</i>.
/// </summary>
public record HnaBatchItemModel
{
    /// <summary>
    /// Sentinel record aktif legacy: nilai <c>fd_tgl_expired = '3000-01-01'</c>
    /// menandakan baris HNA ini adalah snapshot yang sedang berlaku.
    /// </summary>
    public static readonly DateTime TglExpiredDefault = AppConst.DEF_DATE;

    public HnaBatchItemModel(
        string hnaBatchId,
        int noUrut,
        string barangId,
        decimal hnaAkhir,
        decimal hnaRata,
        decimal hnaMax,
        decimal hnaGen,
        decimal hnaGenRi,
        decimal diskonGen,
        DateTime tglBerlaku,
        DateTime tglExpired,
        bool isVoid)
    {
        HnaBatchId = hnaBatchId;
        NoUrut = noUrut;
        BarangId = barangId;
        HnaAkhir = hnaAkhir;
        HnaRata = hnaRata;
        HnaMax = hnaMax;
        HnaGen = hnaGen;
        HnaGenRi = hnaGenRi;
        DiskonGen = diskonGen;
        TglBerlaku = tglBerlaku;
        TglExpired = tglExpired;
        IsVoid = isVoid;
        Validate();
    }

    /// <summary>
    /// Membuat baris HNA baru. Baris baru secara default berstatus aktif
    /// (<see cref="TglExpiredDefault"/>); tanggal kedaluwarsa lama dihitung oleh DAL saat
    /// penerbitan batch berikutnya.
    /// </summary>
    public static HnaBatchItemModel Create(
        string hnaBatchId,
        int noUrut,
        string barangId,
        decimal hnaAkhir,
        decimal hnaRata,
        decimal hnaMax,
        decimal hnaGen,
        decimal diskonGen,
        DateTime tglBerlaku,
        DateTime? tglExpired = null,
        decimal hnaGenRi = 0m)
        => new(
            hnaBatchId,
            noUrut,
            barangId,
            hnaAkhir,
            hnaRata,
            hnaMax,
            hnaGen,
            hnaGenRi,
            diskonGen,
            tglBerlaku,
            tglExpired ?? TglExpiredDefault,
            false);

    public string HnaBatchId { get; init; }

    /// <summary>Nomor urut baris, dipetakan ke kolom legacy <c>fn_no_urut</c>.</summary>
    public int NoUrut { get; private set; }

    /// <summary>Kode barang persediaan, dipetakan ke kolom legacy <c>fs_kd_barang</c>.</summary>
    public string BarangId { get; init; }

    /// <summary>HNA dari HPP faktur pengadaan terakhir (<c>fn_hna_akhir</c>).</summary>
    public decimal HnaAkhir { get; private set; }

    /// <summary>HNA rata-rata aritmatika HPP faktur pengadaan (<c>fn_hna_rata</c>).</summary>
    public decimal HnaRata { get; private set; }

    /// <summary>HNA HPP pembelian tertinggi (<c>fn_hna_max</c>).</summary>
    public decimal HnaMax { get; private set; }

    /// <summary>Nilai HNA aktif hasil komputasi (<c>fn_hna_gen</c>).</summary>
    public decimal HnaGen { get; private set; }

    /// <summary>Nilai HNA aktif khusus rawat inap (<c>fn_hna_gen_ri</c>).</summary>
    public decimal HnaGenRi { get; private set; }

    /// <summary>Diskon distributor pada faktur (<c>fn_diskon</c>).</summary>
    public decimal DiskonGen { get; private set; }

    /// <summary>Tanggal mulai berlaku resmi (<c>fd_tgl_berlaku</c>).</summary>
    public DateTime TglBerlaku { get; private set; }

    /// <summary>
    /// Tanggal kedaluwarsa nilai HNA (<c>fd_tgl_expired</c>).
    /// <see cref="TglExpiredDefault"/> menandakan record aktif saat ini.
    /// </summary>
    public DateTime TglExpired { get; private set; }

    /// <summary>Penanda void baris (<c>fb_void</c>).</summary>
    public bool IsVoid { get; private set; }

    /// <summary>True bila baris ini adalah snapshot HNA aktif yang sedang berlaku.</summary>
    public bool IsActive => !IsVoid && TglExpired.Date == TglExpiredDefault.Date;

    /// <summary>
    /// Menegakkan invarian domain: nilai moneter tidak boleh negatif dan rentang tanggal berlaku valid.
    /// </summary>
    public void Validate()
    {
        if (NoUrut <= 0)
            throw new ArgumentException("NoUrut item HNA harus lebih besar dari 0.", nameof(NoUrut));

        if (string.IsNullOrWhiteSpace(BarangId))
            throw new ArgumentException("Barang ID item HNA harus diisi.", nameof(BarangId));

        if (HnaAkhir < 0m)
            throw new ArgumentException("HnaAkhir tidak boleh bernilai negatif.", nameof(HnaAkhir));

        if (HnaRata < 0m)
            throw new ArgumentException("HnaRata tidak boleh bernilai negatif.", nameof(HnaRata));

        if (HnaMax < 0m)
            throw new ArgumentException("HnaMax tidak boleh bernilai negatif.", nameof(HnaMax));

        if (HnaGen < 0m)
            throw new ArgumentException("HnaGen tidak boleh bernilai negatif.", nameof(HnaGen));

        if (HnaGenRi < 0m)
            throw new ArgumentException("HnaGenRi tidak boleh bernilai negatif.", nameof(HnaGenRi));

        if (DiskonGen < 0m)
            throw new ArgumentException("DiskonGen tidak boleh bernilai negatif.", nameof(DiskonGen));

        if (TglBerlaku == default || TglBerlaku.Date == TglExpiredDefault.Date)
            throw new ArgumentException("TglBerlaku harus berupa tanggal berlaku yang valid.", nameof(TglBerlaku));

        if (TglExpired.Date < TglBerlaku.Date)
            throw new ArgumentException("TglExpired tidak boleh lebih awal dari TglBerlaku.", nameof(TglExpired));
    }

    internal void VoidLine() => IsVoid = true;
}
