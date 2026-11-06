using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.BrgContext.PricingPolicyFeature;

public class BrgHargaJualDal : IBrgHargaJualDal
{
    private readonly DatabaseOptions _opt;

    public BrgHargaJualDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public TipeBrgPricingConfig ResolveTipeBrg(string tipeJaminanId)
    {
        const string sql = """
            SELECT TOP 1
                ISNULL(tb.fs_kd_tipe_barang, r.ResolvedTipeId) AS fs_kd_tipe_barang,
                ISNULL(tb.fs_nm_tipe_barang, r.ResolvedTipeId) AS fs_nm_tipe_barang,
                ISNULL(tb.fn_profit, 0) AS fn_profit,
                ISNULL(tb.fn_biaya_per_barang, 0) AS fn_biaya_per_barang,
                ISNULL(tb.fn_tax, 0) AS fn_tax,
                ISNULL(tb.fn_diskon, 0) AS fn_diskon
            FROM (
                SELECT COALESCE(
                    NULLIF(LTRIM(RTRIM(tj.fs_kd_tipe_barang)), ''),
                    NULLIF(LTRIM(RTRIM(j.fs_kd_tipe_brg_rawat_jalan)), ''),
                    'RJ'
                ) AS ResolvedTipeId
                FROM (SELECT @TipeJaminanId AS fs_kd_tipe_jaminan) p
                LEFT JOIN ta_tipe_jaminan tj ON tj.fs_kd_tipe_jaminan = p.fs_kd_tipe_jaminan
                LEFT JOIN ta_jaminan j ON tj.fs_kd_jaminan = j.fs_kd_jaminan
            ) r
            LEFT JOIN tb_tipe_barang tb ON tb.fs_kd_tipe_barang = r.ResolvedTipeId;
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var row = conn.QueryFirstOrDefault<TipeBrgQueryDto>(sql, new
        {
            TipeJaminanId = (tipeJaminanId ?? string.Empty).Trim()
        });

        if (row == null)
        {
            return new TipeBrgPricingConfig(
                new TipeBrgReff("RJ", "RAWAT JALAN"),
                DefaultProfit: 0m,
                DefaultBiayaPerBarang: 0m,
                DefaultTax: 0m,
                DefaultDiskon: 0m);
        }

        var code = string.IsNullOrWhiteSpace(row.fs_kd_tipe_barang) ? "RJ" : row.fs_kd_tipe_barang.Trim();
        var name = string.IsNullOrWhiteSpace(row.fs_nm_tipe_barang) ? code : row.fs_nm_tipe_barang.Trim();

        return new TipeBrgPricingConfig(
            new TipeBrgReff(code, name),
            DefaultProfit: row.fn_profit,
            DefaultBiayaPerBarang: row.fn_biaya_per_barang,
            DefaultTax: row.fn_tax,
            DefaultDiskon: row.fn_diskon);
    }

    public IReadOnlyDictionary<string, BrgBatchPricingData> GetBatchPricingData(
        IEnumerable<string> brgIds,
        string tipeBrgId,
        string layananId = "")
    {
        if (brgIds == null)
            return new Dictionary<string, BrgBatchPricingData>();

        var idList = brgIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (idList.Count == 0)
            return new Dictionary<string, BrgBatchPricingData>();

        const string sql = """
            WITH LatestHna AS (
                SELECT 
                    fs_kd_barang,
                    fn_hna_akhir,
                    fn_hna_gen,
                    ROW_NUMBER() OVER (PARTITION BY fs_kd_barang ORDER BY fs_kd_trs DESC) AS rn
                FROM TB_TRS_HNA2
                WHERE fb_void = 0 AND fs_kd_barang IN @BrgIds
            ),
            MaxStok AS (
                SELECT 
                    fs_kd_barang,
                    MAX(fn_hpp) AS fn_hpp_max,
                    MAX(CASE WHEN fs_kd_layanan = @LayananId THEN fn_hpp ELSE 0 END) AS fn_hpp_layanan
                FROM tb_stok
                WHERE fs_kd_barang IN @BrgIds
                GROUP BY fs_kd_barang
            ),
            BarangTipe AS (
                SELECT 
                    fs_kd_barang,
                    fn_harga_jual,
                    fn_profit,
                    fn_biaya,
                    fn_tax,
                    fn_diskon
                FROM TB_BARANG_TIPE
                WHERE fs_kd_tipe_harga = @TipeBrgId AND fs_kd_barang IN @BrgIds
            )
            SELECT
                COALESCE(bt.fs_kd_barang, h.fs_kd_barang, s.fs_kd_barang) AS BrgId,
                CAST(CASE WHEN bt.fs_kd_barang IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasBarangTipe,
                ISNULL(bt.fn_harga_jual, 0) AS HargaJual,
                ISNULL(bt.fn_profit, 0) AS Profit,
                ISNULL(bt.fn_biaya, 0) AS Biaya,
                ISNULL(bt.fn_tax, 0) AS Tax,
                ISNULL(bt.fn_diskon, 0) AS Diskon,
                ISNULL(h.fn_hna_gen, 0) AS HnaGen,
                ISNULL(h.fn_hna_akhir, 0) AS HnaAkhir,
                ISNULL(CASE WHEN s.fn_hpp_layanan > 0 THEN s.fn_hpp_layanan ELSE s.fn_hpp_max END, 0) AS HppStok
            FROM BarangTipe bt
            FULL OUTER JOIN (SELECT * FROM LatestHna WHERE rn = 1) h ON bt.fs_kd_barang = h.fs_kd_barang
            FULL OUTER JOIN MaxStok s ON COALESCE(bt.fs_kd_barang, h.fs_kd_barang) = s.fs_kd_barang;
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Query<BrgBatchPricingRowDto>(sql, new
        {
            BrgIds = idList,
            TipeBrgId = string.IsNullOrWhiteSpace(tipeBrgId) ? "RJ" : tipeBrgId.Trim(),
            LayananId = (layananId ?? string.Empty).Trim()
        });

        var result = new Dictionary<string, BrgBatchPricingData>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.BrgId))
                continue;

            result[row.BrgId.Trim()] = new BrgBatchPricingData(
                BrgId: row.BrgId.Trim(),
                HasBarangTipe: row.HasBarangTipe,
                HargaJual: row.HargaJual,
                Profit: row.Profit,
                Biaya: row.Biaya,
                Tax: row.Tax,
                Diskon: row.Diskon,
                HnaGen: row.HnaGen,
                HnaAkhir: row.HnaAkhir,
                HppStok: row.HppStok
            );
        }

        return result;
    }

    private class TipeBrgQueryDto
    {
        public string fs_kd_tipe_barang { get; set; } = string.Empty;
        public string fs_nm_tipe_barang { get; set; } = string.Empty;
        public decimal fn_profit { get; set; }
        public decimal fn_biaya_per_barang { get; set; }
        public decimal fn_tax { get; set; }
        public decimal fn_diskon { get; set; }
    }

    private class BrgBatchPricingRowDto
    {
        public string BrgId { get; set; } = string.Empty;
        public bool HasBarangTipe { get; set; }
        public decimal HargaJual { get; set; }
        public decimal Profit { get; set; }
        public decimal Biaya { get; set; }
        public decimal Tax { get; set; }
        public decimal Diskon { get; set; }
        public decimal HnaGen { get; set; }
        public decimal HnaAkhir { get; set; }
        public decimal HppStok { get; set; }
    }
}
