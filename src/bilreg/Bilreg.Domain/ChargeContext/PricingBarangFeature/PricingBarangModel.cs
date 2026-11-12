using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.ChargeContext.PricingBarangFeature;

/// <summary>
/// Aggregate root parameter harga jual barang persediaan (tabel legacy <c>tb_barang</c> kolom
/// <c>fn_het</c> dan <c>fs_metode_gen_hna</c>) beserta matriks varian harga per tipe penjamin
/// (<c>tb_barang_tipe</c>). Menegakkan invarian prioritas mutlak Override Harga Jual Fix dan
/// pemotongan wajib terhadap plafon legal Harga Eceran Tertinggi (HET) (TD-03).
/// </summary>
public record PricingBarangModel : IPricingBarangKey
{
    private readonly List<ItemPricingVariantType> _variants;

    #region CREATION

    public PricingBarangModel(
        string barangId,
        decimal hetNominal,
        MetodeGenHnaEnum metodeGenHna,
        AuditTrailType auditTrail,
        IEnumerable<ItemPricingVariantType> variants)
    {
        Guard.Against.NullOrWhiteSpace(barangId, nameof(barangId));
        Guard.Against.Negative(hetNominal, nameof(hetNominal));

        BarangId = barangId;
        HetNominal = hetNominal;
        MetodeGenHna = metodeGenHna;
        AuditTrail = auditTrail;
        _variants = variants?.OrderBy(x => x.TipeHargaId, StringComparer.Ordinal).ToList() ?? [];
    }

    public static PricingBarangModel Create(
        string barangId,
        decimal hetNominal,
        MetodeGenHnaEnum metodeGenHna,
        string auditUserId,
        DateTime createdAt = default)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId, nameof(auditUserId));

        return new PricingBarangModel(
            barangId,
            hetNominal,
            metodeGenHna,
            AuditTrailType.Create(auditUserId, createdAt),
            []);
    }

    public static PricingBarangModel Default => new(
        barangId: "-",
        hetNominal: 0m,
        metodeGenHna: MetodeGenHnaEnum.Terakhir,
        auditTrail: AuditTrailType.Default,
        variants: []);

    public static IPricingBarangKey Key(string barangId) => Default with { BarangId = barangId };

    #endregion

    #region PROPERTIES

    /// <summary>Kode identitas komoditas barang (kolom legacy <c>fs_kd_barang</c>).</summary>
    public string BarangId { get; init; }

    /// <summary>Plafon legal Harga Eceran Tertinggi (kolom legacy <c>fn_het</c>). <c>0</c> berarti tanpa plafon.</summary>
    public decimal HetNominal { get; init; }

    /// <summary>Metode agregasi HNA default (kolom legacy <c>fs_metode_gen_hna</c>).</summary>
    public MetodeGenHnaEnum MetodeGenHna { get; init; }

    public AuditTrailType AuditTrail { get; init; }

    /// <summary>Matriks varian harga per tipe penjamin.</summary>
    public IReadOnlyCollection<ItemPricingVariantType> Variants => _variants;

    #endregion

    #region BEHAVIOUR

    /// <summary>
    /// Menambahkan varian baru atau memperbarui varian yang sudah ada untuk <paramref name="tipeHargaId"/>.
    /// </summary>
    public PricingBarangModel AddOrUpdateVariant(
        string tipeHargaId,
        decimal hargaJualFix,
        decimal marginPersen,
        decimal biayaTambahanItem,
        decimal taxPersen,
        decimal diskonDefaultPersen,
        bool isActive)
    {
        Guard.Against.NullOrWhiteSpace(tipeHargaId, nameof(tipeHargaId));

        var variant = new ItemPricingVariantType(
            tipeHargaId,
            hargaJualFix,
            marginPersen,
            biayaTambahanItem,
            taxPersen,
            diskonDefaultPersen,
            isActive);

        var nextVariants = _variants
            .Where(x => x.TipeHargaId != tipeHargaId)
            .ToList();
        nextVariants.Add(variant);

        return new PricingBarangModel(BarangId, HetNominal, MetodeGenHna, AuditTrail, nextVariants);
    }

    /// <summary>
    /// Menghasilkan harga jual satuan final yang deterministik untuk tipe harga tertentu,
    /// dengan prioritas Override Harga Jual Fix dan pemotongan wajib terhadap plafon HET (TD-03).
    /// </summary>
    /// <param name="tipeHargaId">Kode tipe harga / tipe penjamin.</param>
    /// <param name="hna">Harga Netto Apotek aktif per satuan terkecil.</param>
    public HargaJualResultType CalculatePrice(string tipeHargaId, decimal hna)
    {
        Guard.Against.NullOrWhiteSpace(tipeHargaId, nameof(tipeHargaId));
        Guard.Against.Negative(hna, nameof(hna));

        var variant = _variants.FirstOrDefault(x => x.TipeHargaId == tipeHargaId)
            ?? throw new KeyNotFoundException(
                $"Varian tipe harga '{tipeHargaId}' tidak ditemukan pada barang '{BarangId}'.");

        var hargaJualTerpilih = variant.CalculateUnitPrice(hna);
        var isHetCapped = HetNominal > 0m && hargaJualTerpilih > HetNominal;
        var hargaJualFinal = isHetCapped ? HetNominal : hargaJualTerpilih;

        return new HargaJualResultType(hargaJualTerpilih, hargaJualFinal, isHetCapped);
    }

    #endregion
}

/// <summary>
/// Hasil kalkulasi harga jual satuan final.
/// </summary>
/// <param name="HargaJualTerpilih">Harga hasil formula HNA/margin atau Override Harga Jual Fix, sebelum pemotongan HET.</param>
/// <param name="HargaJualFinal">Harga jual satuan efektif yang dibebankan ke pasien (tidak melampaui HET bila <c>HET &gt; 0</c>).</param>
/// <param name="IsHetCapped">Menandakan harga dipotong menjadi senilai HET akibat melampaui plafon legal.</param>
public record HargaJualResultType(decimal HargaJualTerpilih, decimal HargaJualFinal, bool IsHetCapped);
