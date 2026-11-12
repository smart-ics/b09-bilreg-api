using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

public interface IHnaBatchItemDal :
    IListData<HnaBatchItemDto, IHnaBatchKey>
{
    /// <summary>
    /// Nilai <c>fn_hna_gen</c> snapshot aktif (<c>fd_tgl_expired = '3000-01-01'</c>) untuk suatu barang
    /// pada tanggal tertentu; <c>null</c> bila tidak ada.
    /// </summary>
    decimal? GetActiveHna(string barangId, DateTime tgl);

    /// <summary>
    /// Menyisipkan baris HNA baru sekaligus mengedaluwarsakan snapshot aktif lama pada
    /// <b>koneksi dan transaksi yang sama</b> (menjaga invarian <i>effective-dating</i> atomik).
    /// </summary>
    void SaveBatch(SqlConnection conn, SqlTransaction tx, string hnaBatchId, IEnumerable<HnaBatchItemDto> items);
}

/// <summary>
/// Data access baris HNA per barang pada tabel legacy <c>tb_trs_hna2</c>.
/// </summary>
public class HnaBatchItemDal : IHnaBatchItemDal
{
    private const string ActiveExpiredDate = "3000-01-01";
    private readonly DatabaseOptions _opt;

    public HnaBatchItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void SaveBatch(SqlConnection conn, SqlTransaction tx, string hnaBatchId, IEnumerable<HnaBatchItemDto> items)
    {
        foreach (var item in items ?? [])
        {
            // Invarian effective-dating atomik (ARCHITECTURE TD-04): snapshot aktif lama barang
            // dikedaluwarsakan menjadi TglBerlaku - 1 hari sebelum baris aktif baru disisipkan.
            var tglExpiredLama = HnaBatchItemDto.ParseDate(item.fd_tgl_berlaku).AddDays(-1);
            ExpireActive(conn, tx, item.fs_kd_barang, hnaBatchId, tglExpiredLama);
            Insert(conn, tx, item);
        }
    }

    public IEnumerable<HnaBatchItemDto> ListData(IHnaBatchKey key)
    {
        const string sql = """
            SELECT
                fs_kd_trs,
                fn_no_urut,
                fs_kd_barang,
                fn_hna_akhir,
                fn_hna_rata,
                fn_hna_max,
                fn_hna_gen,
                fn_diskon,
                ISNULL(fn_hna_gen_ri, 0) AS fn_hna_gen_ri,
                fd_tgl_berlaku,
                fd_tgl_expired,
                fb_void,
                CRTTGL,
                CRTJAM,
                CRTUSR,
                UPDTGL,
                UPDJAM,
                UPDUSR
            FROM tb_trs_hna2
            WHERE fs_kd_trs = @fs_kd_trs
            ORDER BY fn_no_urut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.HnaBatchId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<HnaBatchItemDto>(sql, dp);
    }

    public decimal? GetActiveHna(string barangId, DateTime tgl)
    {
        const string sql = """
            SELECT TOP 1 fn_hna_gen
            FROM tb_trs_hna2
            WHERE fs_kd_barang = @fs_kd_barang
              AND fb_void = 0
              AND fd_tgl_expired = '3000-01-01'
              AND fd_tgl_berlaku <= @fd_tgl
            ORDER BY fd_tgl_berlaku DESC, fn_no_urut DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", barangId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl", HnaBatchDto.FormatDate(tgl), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var row = conn.Read<HnaGenRow>(sql, dp)?.FirstOrDefault();
        return row?.fn_hna_gen;
    }

    private static void ExpireActive(
        SqlConnection conn, SqlTransaction tx, string barangId, string newHnaBatchId, DateTime tglExpiredLama)
    {
        const string sql = """
            UPDATE tb_trs_hna2
            SET    fd_tgl_expired = @TglExpiredLama
            WHERE  fs_kd_barang = @BarangId
              AND  fd_tgl_expired = '3000-01-01'
              AND  fs_kd_trs <> @TrsHnaBaru
              AND  fb_void = 0
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TglExpiredLama", HnaBatchDto.FormatDate(tglExpiredLama), SqlDbType.VarChar);
        dp.AddParam("@BarangId", barangId, SqlDbType.VarChar);
        dp.AddParam("@TrsHnaBaru", newHnaBatchId, SqlDbType.VarChar);

        conn.Execute(sql, dp, tx);
    }

    private static void Insert(SqlConnection conn, SqlTransaction tx, HnaBatchItemDto item)
    {
        const string sql = """
            INSERT INTO tb_trs_hna2 (
                fs_kd_trs,
                fn_no_urut,
                fs_kd_barang,
                fn_hna_akhir,
                fn_hna_rata,
                fn_hna_max,
                fn_hna_gen,
                fn_diskon,
                fn_hna_gen_ri,
                fd_tgl_berlaku,
                fd_tgl_expired,
                fb_void,
                CRTTGL,
                CRTJAM,
                CRTUSR,
                UPDTGL,
                UPDJAM,
                UPDUSR
            ) VALUES (
                @fs_kd_trs,
                @fn_no_urut,
                @fs_kd_barang,
                @fn_hna_akhir,
                @fn_hna_rata,
                @fn_hna_max,
                @fn_hna_gen,
                @fn_diskon,
                @fn_hna_gen_ri,
                @fd_tgl_berlaku,
                @fd_tgl_expired,
                @fb_void,
                @CRTTGL,
                @CRTJAM,
                @CRTUSR,
                @UPDTGL,
                @UPDJAM,
                @UPDUSR
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", item.fs_kd_trs, SqlDbType.VarChar);
        dp.AddParam("@fn_no_urut", item.fn_no_urut, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_barang", item.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@fn_hna_akhir", item.fn_hna_akhir, SqlDbType.Decimal);
        dp.AddParam("@fn_hna_rata", item.fn_hna_rata, SqlDbType.Decimal);
        dp.AddParam("@fn_hna_max", item.fn_hna_max, SqlDbType.Decimal);
        dp.AddParam("@fn_hna_gen", item.fn_hna_gen, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon", item.fn_diskon, SqlDbType.Decimal);
        dp.AddParam("@fn_hna_gen_ri", item.fn_hna_gen_ri, SqlDbType.Decimal);
        dp.AddParam("@fd_tgl_berlaku", item.fd_tgl_berlaku, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_expired", item.fd_tgl_expired, SqlDbType.VarChar);
        dp.AddParam("@fb_void", item.fb_void, SqlDbType.Bit);
        dp.AddParam("@CRTTGL", item.CRTTGL, SqlDbType.VarChar);
        dp.AddParam("@CRTJAM", item.CRTJAM, SqlDbType.VarChar);
        dp.AddParam("@CRTUSR", item.CRTUSR, SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", item.UPDTGL, SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", item.UPDJAM, SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", item.UPDUSR, SqlDbType.VarChar);

        conn.Execute(sql, dp, tx);
    }

    private record HnaGenRow(decimal fn_hna_gen);
}
