using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.ApotekContext.PenjualanFeature;

/// <summary>
/// Implementasi Dapper DAL untuk 68 kolom tabel tb_trs_dobill_umum.
/// Menyediakan operasi Insert, Update, SoftVoid, Delete, dan queries dengan filter void aktif.
/// </summary>
public class PenjualanDal : IPenjualanDal
{
    private const string ApiUser = "BILREG-API";
    private readonly DatabaseOptions _opt;

    public PenjualanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PenjualanDto dto, SqlConnection? conn = null, SqlTransaction? tx = null)
    {
        const string sql = """
            INSERT INTO tb_trs_dobill_umum (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas,
                fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                fs_kd_resep, fs_kd_layanan_resep, fs_kd_petugas_medis, fs_kd_layanan,
                fs_kd_reg, fs_nm_pasien, fs_alm_pasien, fs_alm2_pasien, fs_kota_pasien, fs_sex,
                fn_sub_total, fn_diskon_lain, fn_biaya_lain, fn_grand_total, fn_kas, fn_sisa,
                fs_kd_rek_kas, fs_kd_rek_sisa, fs_kd_tipe_barang, fs_kd_trs_kwitansi,
                fn_sum_sub_total, fn_sum_biaya, fn_sum_tax_rupiah, fs_kd_petugas_kasir,
                fn_bank, fn_kartu_kredit, fs_kd_jenis_bank, fs_kd_jenis_kartu_kredit,
                fs_kd_kelas, fs_kd_rek_bank, fs_kd_rek_diskon_lain, fs_no_kartu_kredit,
                fs_kd_rek_kartu_kredit, fs_kd_rek_pdpt, fs_kd_trs_charge, fs_kd_trs_deposit,
                fs_kd_tipe_jaminan, fs_no_bank, fn_pembulatan, fs_kd_trs_gen, fn_bulat,
                fs_kd_trs_mutasi, fd_tgl_serah, fs_jam_serah, fs_kd_petugas_serah,
                fs_catatan_lain, fb_reserved_order, fs_no_dok, fn_genkeu, fb_odd,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR,
                fd_tgl_jam_trs
            ) VALUES (
                @fs_kd_trs, @fd_tgl_trs, @fs_jam_trs, @fs_kd_petugas,
                @fd_tgl_void, @fs_jam_void, @fs_kd_petugas_void,
                @fs_kd_resep, @fs_kd_layanan_resep, @fs_kd_petugas_medis, @fs_kd_layanan,
                @fs_kd_reg, @fs_nm_pasien, @fs_alm_pasien, @fs_alm2_pasien, @fs_kota_pasien, @fs_sex,
                @fn_sub_total, @fn_diskon_lain, @fn_biaya_lain, @fn_grand_total, @fn_kas, @fn_sisa,
                @fs_kd_rek_kas, @fs_kd_rek_sisa, @fs_kd_tipe_barang, @fs_kd_trs_kwitansi,
                @fn_sum_sub_total, @fn_sum_biaya, @fn_sum_tax_rupiah, @fs_kd_petugas_kasir,
                @fn_bank, @fn_kartu_kredit, @fs_kd_jenis_bank, @fs_kd_jenis_kartu_kredit,
                @fs_kd_kelas, @fs_kd_rek_bank, @fs_kd_rek_diskon_lain, @fs_no_kartu_kredit,
                @fs_kd_rek_kartu_kredit, @fs_kd_rek_pdpt, @fs_kd_trs_charge, @fs_kd_trs_deposit,
                @fs_kd_tipe_jaminan, @fs_no_bank, @fn_pembulatan, @fs_kd_trs_gen, @fn_bulat,
                @fs_kd_trs_mutasi, @fd_tgl_serah, @fs_jam_serah, @fs_kd_petugas_serah,
                @fs_catatan_lain, @fb_reserved_order, @fs_no_dok, @fn_genkeu, @fb_odd,
                @CRTTGL, @CRTJAM, @CRTIPA, @CRTVER, @CRTUSR,
                @UPDTGL, @UPDJAM, @UPDIPA, @UPDVER, @UPDUSR,
                @fd_tgl_jam_trs
            )
            """;

        ExecuteSql(sql, dto, conn, tx);
    }

    public void Update(PenjualanDto dto, SqlConnection? conn = null, SqlTransaction? tx = null)
    {
        const string sql = """
            UPDATE tb_trs_dobill_umum
            SET
                fd_tgl_trs = @fd_tgl_trs,
                fs_jam_trs = @fs_jam_trs,
                fs_kd_petugas = @fs_kd_petugas,
                fd_tgl_void = @fd_tgl_void,
                fs_jam_void = @fs_jam_void,
                fs_kd_petugas_void = @fs_kd_petugas_void,
                fs_kd_resep = @fs_kd_resep,
                fs_kd_layanan_resep = @fs_kd_layanan_resep,
                fs_kd_petugas_medis = @fs_kd_petugas_medis,
                fs_kd_layanan = @fs_kd_layanan,
                fs_kd_reg = @fs_kd_reg,
                fs_nm_pasien = @fs_nm_pasien,
                fs_alm_pasien = @fs_alm_pasien,
                fs_alm2_pasien = @fs_alm2_pasien,
                fs_kota_pasien = @fs_kota_pasien,
                fs_sex = @fs_sex,
                fn_sub_total = @fn_sub_total,
                fn_diskon_lain = @fn_diskon_lain,
                fn_biaya_lain = @fn_biaya_lain,
                fn_grand_total = @fn_grand_total,
                fn_kas = @fn_kas,
                fn_sisa = @fn_sisa,
                fs_kd_rek_kas = @fs_kd_rek_kas,
                fs_kd_rek_sisa = @fs_kd_rek_sisa,
                fs_kd_tipe_barang = @fs_kd_tipe_barang,
                fs_kd_trs_kwitansi = @fs_kd_trs_kwitansi,
                fn_sum_sub_total = @fn_sum_sub_total,
                fn_sum_biaya = @fn_sum_biaya,
                fn_sum_tax_rupiah = @fn_sum_tax_rupiah,
                fs_kd_petugas_kasir = @fs_kd_petugas_kasir,
                fn_bank = @fn_bank,
                fn_kartu_kredit = @fn_kartu_kredit,
                fs_kd_jenis_bank = @fs_kd_jenis_bank,
                fs_kd_jenis_kartu_kredit = @fs_kd_jenis_kartu_kredit,
                fs_kd_kelas = @fs_kd_kelas,
                fs_kd_rek_bank = @fs_kd_rek_bank,
                fs_kd_rek_diskon_lain = @fs_kd_rek_diskon_lain,
                fs_no_kartu_kredit = @fs_no_kartu_kredit,
                fs_kd_rek_kartu_kredit = @fs_kd_rek_kartu_kredit,
                fs_kd_rek_pdpt = @fs_kd_rek_pdpt,
                fs_kd_trs_charge = @fs_kd_trs_charge,
                fs_kd_trs_deposit = @fs_kd_trs_deposit,
                fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan,
                fs_no_bank = @fs_no_bank,
                fn_pembulatan = @fn_pembulatan,
                fs_kd_trs_gen = @fs_kd_trs_gen,
                fn_bulat = @fn_bulat,
                fs_kd_trs_mutasi = @fs_kd_trs_mutasi,
                fd_tgl_serah = @fd_tgl_serah,
                fs_jam_serah = @fs_jam_serah,
                fs_kd_petugas_serah = @fs_kd_petugas_serah,
                fs_catatan_lain = @fs_catatan_lain,
                fb_reserved_order = @fb_reserved_order,
                fs_no_dok = @fs_no_dok,
                fn_genkeu = @fn_genkeu,
                fb_odd = @fb_odd,
                UPDTGL = @UPDTGL,
                UPDJAM = @UPDJAM,
                UPDIPA = @UPDIPA,
                UPDVER = @UPDVER,
                UPDUSR = @UPDUSR,
                fd_tgl_jam_trs = @fd_tgl_jam_trs
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;

        ExecuteSql(sql, dto, conn, tx);
    }

    public void SoftVoid(
        string penjualanId,
        string tglVoid,
        string jamVoid,
        string userVoid,
        string reason,
        SqlConnection? conn = null,
        SqlTransaction? tx = null)
    {
        const string sql = """
            UPDATE tb_trs_dobill_umum
            SET
                fd_tgl_void = @tglVoid,
                fs_jam_void = @jamVoid,
                fs_kd_petugas_void = @userVoid,
                fs_catatan_lain = @reason,
                UPDTGL = @tglVoid,
                UPDJAM = @jamVoid,
                UPDUSR = @userVoid
            WHERE
                fs_kd_trs = @penjualanId
            """;

        var dp = new { penjualanId, tglVoid, jamVoid, userVoid, reason };
        ExecuteSql(sql, dp, conn, tx);
    }

    public void Delete(IPenjualanKey key, SqlConnection? conn = null, SqlTransaction? tx = null)
    {
        const string sql = """
            DELETE FROM tb_trs_dobill_umum
            WHERE fs_kd_trs = @PenjualanId
            """;

        var dp = new { key.PenjualanId };
        ExecuteSql(sql, dp, conn, tx);
    }

    public PenjualanDto? GetData(IPenjualanKey key, SqlConnection? conn = null, SqlTransaction? tx = null)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fs_kd_trs = @PenjualanId
            """;

        var dp = new { key.PenjualanId };
        return QuerySingle(sql, dp, conn, tx);
    }

    public IEnumerable<PenjualanDto> ListByReg(string regId)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fs_kd_reg = @regId
                AND (aa.fd_tgl_void = '' OR aa.fd_tgl_void = '3000-01-01')
            ORDER BY
                aa.fd_tgl_trs DESC, aa.fs_jam_trs DESC
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PenjualanDto>(sql, new { regId });
    }

    public IEnumerable<PenjualanDto> ListWorklist(string tglTrs)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fd_tgl_trs = @tglTrs
                AND (aa.fd_tgl_void = '' OR aa.fd_tgl_void = '3000-01-01')
            ORDER BY
                aa.fs_jam_trs DESC
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PenjualanDto>(sql, new { tglTrs });
    }

    public IEnumerable<PenjualanDto> ListByResep(string resepId)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fs_kd_resep = @resepId
                AND (aa.fd_tgl_void = '' OR aa.fd_tgl_void = '3000-01-01')
            ORDER BY
                aa.fd_tgl_trs DESC, aa.fs_jam_trs DESC
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PenjualanDto>(sql, new { resepId });
    }

    private void ExecuteSql(string sql, object param, SqlConnection? conn, SqlTransaction? tx)
    {
        if (conn != null)
        {
            conn.Execute(sql, param, tx);
        }
        else
        {
            using var localConn = new SqlConnection(ConnStringHelper.Get(_opt));
            localConn.Execute(sql, param);
        }
    }

    private PenjualanDto? QuerySingle(string sql, object param, SqlConnection? conn, SqlTransaction? tx)
    {
        if (conn != null)
        {
            return conn.QuerySingleOrDefault<PenjualanDto>(sql, param, tx);
        }
        else
        {
            using var localConn = new SqlConnection(ConnStringHelper.Get(_opt));
            return localConn.QuerySingleOrDefault<PenjualanDto>(sql, param);
        }
    }

    private static string SelectClause() => """
        SELECT
            aa.fs_kd_trs,
            aa.fd_tgl_trs,
            aa.fs_jam_trs,
            aa.fs_kd_petugas,
            aa.fd_tgl_void,
            aa.fs_jam_void,
            aa.fs_kd_petugas_void,
            aa.fs_kd_resep,
            aa.fs_kd_layanan_resep,
            aa.fs_kd_petugas_medis,
            aa.fs_kd_layanan,
            aa.fs_kd_reg,
            aa.fs_nm_pasien,
            aa.fs_alm_pasien,
            aa.fs_alm2_pasien,
            aa.fs_kota_pasien,
            aa.fs_sex,
            aa.fn_sub_total,
            aa.fn_diskon_lain,
            aa.fn_biaya_lain,
            aa.fn_grand_total,
            aa.fn_kas,
            aa.fn_sisa,
            aa.fs_kd_rek_kas,
            aa.fs_kd_rek_sisa,
            aa.fs_kd_tipe_barang,
            aa.fs_kd_trs_kwitansi,
            aa.fn_sum_sub_total,
            aa.fn_sum_biaya,
            aa.fn_sum_tax_rupiah,
            aa.fs_kd_petugas_kasir,
            aa.fn_bank,
            aa.fn_kartu_kredit,
            aa.fs_kd_jenis_bank,
            aa.fs_kd_jenis_kartu_kredit,
            aa.fs_kd_kelas,
            aa.fs_kd_rek_bank,
            aa.fs_kd_rek_diskon_lain,
            aa.fs_no_kartu_kredit,
            aa.fs_kd_rek_kartu_kredit,
            aa.fs_kd_rek_pdpt,
            aa.fs_kd_trs_charge,
            aa.fs_kd_trs_deposit,
            aa.fs_kd_tipe_jaminan,
            aa.fs_no_bank,
            aa.fn_pembulatan,
            aa.fs_kd_trs_gen,
            aa.fn_bulat,
            aa.fs_kd_trs_mutasi,
            aa.fd_tgl_serah,
            aa.fs_jam_serah,
            aa.fs_kd_petugas_serah,
            aa.fs_catatan_lain,
            aa.fb_reserved_order,
            aa.fs_no_dok,
            aa.fn_genkeu,
            aa.fb_odd,
            aa.CRTTGL,
            aa.CRTJAM,
            aa.CRTIPA,
            aa.CRTVER,
            aa.CRTUSR,
            aa.UPDTGL,
            aa.UPDJAM,
            aa.UPDIPA,
            aa.UPDVER,
            aa.UPDUSR,
            aa.fd_tgl_jam_trs
        FROM
            tb_trs_dobill_umum aa
        """;
}
