using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.BrgContext.BrgFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

/// <summary>
/// Implementasi konkret domain service <see cref="IItemPricingCalculator"/> (ARCHITECTURE TD-06).
/// Memadukan snapshot HNA aktif (<c>tb_trs_hna2</c>), matriks varian harga (<c>tb_barang_tipe</c>),
/// plafon HET (<c>tb_barang</c>), jasa kefarmasian (<c>tb_tipe_barang</c>), dan rasio konversi
/// satuan (<c>tb_barang_satuan</c>) menjadi satu kontrak komputasi harga deterministik.
/// </summary>
public class ItemPricingCalculator : IItemPricingCalculator
{
    private readonly IPricingBarangRepo _pricingBarangRepo;
    private readonly ITipeHargaRepo _tipeHargaRepo;
    private readonly IHnaBatchRepo _hnaBatchRepo;
    private readonly IBrgSatuanDal _brgSatuanDal;

    public ItemPricingCalculator(
        IPricingBarangRepo pricingBarangRepo,
        ITipeHargaRepo tipeHargaRepo,
        IHnaBatchRepo hnaBatchRepo,
        IBrgSatuanDal brgSatuanDal)
    {
        _pricingBarangRepo = pricingBarangRepo;
        _tipeHargaRepo = tipeHargaRepo;
        _hnaBatchRepo = hnaBatchRepo;
        _brgSatuanDal = brgSatuanDal;
    }

    public Task<ItemPriceCalculationResultDto> CalculateAsync(
        string barangId,
        string tipeHargaId,
        DateTime transactionDate,
        int qty = 1,
        string satuanId = "",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pricingModel = _pricingBarangRepo
            .LoadEntity(PricingBarangModel.Key(barangId))
            .Match(
                onSome: model => model,
                onNone: () => throw new KeyNotFoundException($"Barang '{barangId}' tidak ditemukan pada matriks harga."));

        var variant = pricingModel.Variants.FirstOrDefault(x => x.TipeHargaId == tipeHargaId)
            ?? throw new KeyNotFoundException(
                $"Varian tipe harga '{tipeHargaId}' tidak ditemukan pada barang '{barangId}'.");

        var tipeHarga = _tipeHargaRepo
            .LoadEntity(TipeHargaModel.Key(tipeHargaId))
            .Match(
                onSome: model => model,
                onNone: () => throw new KeyNotFoundException($"Tipe harga '{tipeHargaId}' tidak ditemukan."));

        // ASM-006 / EX-PRC-01: snapshot HNA aktif wajib ada, kecuali override Harga Jual Fix
        // (> 0) sudah menetapkan harga sementara sehingga formula HNA x margin diabaikan.
        var activeHna = _hnaBatchRepo.GetActiveHna(barangId, transactionDate);
        if (!activeHna.HasValue && !variant.IsFixedPriceOverride)
            throw new HnaNotFoundException(barangId);

        var hna = activeHna ?? 0m;

        // TD-03 langkah 1-3: formula margin / override Harga Jual Fix + pemotongan wajib HET.
        var harga = pricingModel.CalculatePrice(tipeHargaId, hna);

        // TD-03 langkah 4: konversi satuan dua tingkat (Two-Tier UOM).
        var konversi = ResolveConversion(barangId, satuanId);
        var hargaJualFinal = harga.HargaJualFinal * konversi;

        return Task.FromResult(new ItemPriceCalculationResultDto(
            BarangId: barangId,
            TipeHargaId: tipeHargaId,
            TransactionDate: transactionDate,
            Qty: qty,
            SatuanId: satuanId ?? string.Empty,
            Konversi: konversi,
            HnaApplied: hna,
            MarginPersenApplied: variant.MarginPersen,
            HargaJualFixApplied: variant.HargaJualFix,
            IsFixedPriceApplied: variant.IsFixedPriceOverride,
            IsHetCapped: harga.IsHetCapped,
            HetNominal: pricingModel.HetNominal,
            HargaJualSatuanDasar: harga.HargaJualFinal,
            HargaJualFinal: hargaJualFinal,
            DiskonDefaultPersen: variant.DiskonDefaultPersen,
            BiayaTambahanItem: variant.BiayaTambahanItem,
            BiayaPerObat_r: tipeHarga.BiayaPerObat_r,
            BiayaPerResep_R: tipeHarga.BiayaPerResep_R));
    }

    public RoundingEvaluationResultDto CalculateRounding(
        decimal totalBeforeRounding,
        int roundingIncrement,
        RoundingModeEnum roundingMode)
    {
        var policy = new RoundingPolicyModel(roundingIncrement, roundingMode);
        var result = policy.HitungPembulatan(totalBeforeRounding);

        return new RoundingEvaluationResultDto(
            TotalBeforeRounding: result.TotalSebelum,
            TotalAfterRounding: result.TotalAkhir,
            RoundingDifference: result.SelisihPembulatan,
            RoundingIncrement: roundingIncrement,
            RoundingMode: roundingMode);
    }

    /// <summary>
    /// Menghitung rasio konversi UOM dua tingkat: harga satuan dasar dikalikan rasio
    /// <c>Konversi</c> ketika transaksi memakai satuan kemasan besar (TD-03 langkah 4).
    /// Satuan dasar (terkecil) bernilai rasio <c>1</c>. Bila satuan tidak terpetakan, rasio
    /// dianggap <c>1</c>.
    /// </summary>
    private decimal ResolveConversion(string barangId, string? satuanId)
    {
        if (string.IsNullOrWhiteSpace(satuanId))
            return 1m;

        var satuanList = _brgSatuanDal.ListData(new BrgKey(barangId))?.ToList() ?? [];
        if (satuanList.Count == 0)
            return 1m;

        var requested = satuanList.FirstOrDefault(x => x.fs_kd_satuan == satuanId);
        if (requested is null)
            return 1m;

        var baseUnit = satuanList.OrderBy(x => x.fn_nilai).First();

        if (requested.fs_kd_satuan == baseUnit.fs_kd_satuan)
            return 1m;

        if (baseUnit.fn_nilai <= 0m)
            return requested.fn_nilai > 0m ? requested.fn_nilai : 1m;

        return requested.fn_nilai / baseUnit.fn_nilai;
    }
}
