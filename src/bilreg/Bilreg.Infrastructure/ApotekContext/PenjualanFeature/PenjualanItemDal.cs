using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.ApotekContext.PenjualanFeature;

/// <summary>
/// Implementasi DAL detail penjualan farmasi ke tabel tb_trs_dobill_umum2 menggunakan SqlBulkCopy 54 kolom.
/// </summary>
public class PenjualanItemDal : IPenjualanItemDal
{
    private readonly DatabaseOptions _opt;

    public PenjualanItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void InsertBulk(IEnumerable<PenjualanItemDto> items, SqlConnection? conn = null, SqlTransaction? tx = null)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
            return;

        var shouldCloseLocal = false;
        var activeConn = conn;
        if (activeConn == null)
        {
            activeConn = new SqlConnection(ConnStringHelper.Get(_opt));
            activeConn.Open();
            shouldCloseLocal = true;
        }

        try
        {
            using var bcp = tx != null
                ? new SqlBulkCopy(activeConn, SqlBulkCopyOptions.Default, tx)
                : new SqlBulkCopy(activeConn);

            bcp.DestinationTableName = "dbo.tb_trs_dobill_umum2";
            bcp.BatchSize = itemList.Count;

            // Map all 54 columns precisely
            bcp.ColumnMappings.Add("fs_kd_trs", "fs_kd_trs");
            bcp.ColumnMappings.Add("fs_kd_trs2", "fs_kd_trs2");
            bcp.ColumnMappings.Add("fn_no_urut", "fn_no_urut");
            bcp.ColumnMappings.Add("fb_void", "fb_void");
            bcp.ColumnMappings.Add("fs_kd_barang", "fs_kd_barang");
            bcp.ColumnMappings.Add("fs_nm_barang", "fs_nm_barang");
            bcp.ColumnMappings.Add("fs_kd_tipe_barang", "fs_kd_tipe_barang");
            bcp.ColumnMappings.Add("fb_racik", "fb_racik");
            bcp.ColumnMappings.Add("fb_komponen", "fb_komponen");
            bcp.ColumnMappings.Add("fs_kd_racik", "fs_kd_racik");
            bcp.ColumnMappings.Add("fn_qty_racik", "fn_qty_racik");
            bcp.ColumnMappings.Add("fs_racik_qty", "fs_racik_qty");
            bcp.ColumnMappings.Add("fn_qty_barang", "fn_qty_barang");
            bcp.ColumnMappings.Add("fs_nm_satuan", "fs_nm_satuan");
            bcp.ColumnMappings.Add("fn_harga_satuan", "fn_harga_satuan");
            bcp.ColumnMappings.Add("fn_diskon", "fn_diskon");
            bcp.ColumnMappings.Add("fn_biaya", "fn_biaya");
            bcp.ColumnMappings.Add("fn_sub_total", "fn_sub_total");
            bcp.ColumnMappings.Add("fn_tax_prosen", "fn_tax_prosen");
            bcp.ColumnMappings.Add("fn_tax_rupiah", "fn_tax_rupiah");
            bcp.ColumnMappings.Add("fn_biaya_fee", "fn_biaya_fee");
            bcp.ColumnMappings.Add("fn_total", "fn_total");
            bcp.ColumnMappings.Add("fs_etiket", "fs_etiket");
            bcp.ColumnMappings.Add("fb_asuransi", "fb_asuransi");
            bcp.ColumnMappings.Add("fs_kd_tipe_jaminan", "fs_kd_tipe_jaminan");
            bcp.ColumnMappings.Add("fn_bulat", "fn_bulat");
            bcp.ColumnMappings.Add("fs_kd_satuan", "fs_kd_satuan");
            bcp.ColumnMappings.Add("fn_etiket_qty", "fn_etiket_qty");
            bcp.ColumnMappings.Add("fs_etiket_qty", "fs_etiket_qty");
            bcp.ColumnMappings.Add("fn_etiket_hari", "fn_etiket_hari");
            bcp.ColumnMappings.Add("fs_etiket_kd_pakai", "fs_etiket_kd_pakai");
            bcp.ColumnMappings.Add("fs_etiket_catatan", "fs_etiket_catatan");
            bcp.ColumnMappings.Add("fs_etiket_jenis_obat", "fs_etiket_jenis_obat");
            bcp.ColumnMappings.Add("fs_etiket_kd_satuan_pakai", "fs_etiket_kd_satuan_pakai");
            bcp.ColumnMappings.Add("fd_etiket_cetak", "fd_etiket_cetak");
            bcp.ColumnMappings.Add("fb_belibebas", "fb_belibebas");
            bcp.ColumnMappings.Add("fs_etiket_hari", "fs_etiket_hari");
            bcp.ColumnMappings.Add("fb_paket_tarif", "fb_paket_tarif");
            bcp.ColumnMappings.Add("fn_nilai_klaim", "fn_nilai_klaim");
            bcp.ColumnMappings.Add("fs_waktu_odd", "fs_waktu_odd");
            bcp.ColumnMappings.Add("fn_etiket_cetak", "fn_etiket_cetak");
            bcp.ColumnMappings.Add("CRTTGL", "CRTTGL");
            bcp.ColumnMappings.Add("CRTJAM", "CRTJAM");
            bcp.ColumnMappings.Add("CRTIPA", "CRTIPA");
            bcp.ColumnMappings.Add("CRTVER", "CRTVER");
            bcp.ColumnMappings.Add("CRTUSR", "CRTUSR");
            bcp.ColumnMappings.Add("UPDTGL", "UPDTGL");
            bcp.ColumnMappings.Add("UPDJAM", "UPDJAM");
            bcp.ColumnMappings.Add("UPDIPA", "UPDIPA");
            bcp.ColumnMappings.Add("UPDVER", "UPDVER");
            bcp.ColumnMappings.Add("UPDUSR", "UPDUSR");
            bcp.ColumnMappings.Add("fn_etiket_sms", "fn_etiket_sms");
            bcp.ColumnMappings.Add("fs_etiket_description", "fs_etiket_description");
            bcp.ColumnMappings.Add("fb_biaya_tax", "fb_biaya_tax");

            var dt = PenjualanItemDto.BuildDataTable(itemList);
            bcp.WriteToServer(dt);
        }
        finally
        {
            if (shouldCloseLocal)
                activeConn.Dispose();
        }
    }

    public void SoftVoidItems(
        string penjualanId,
        string tglVoid,
        string jamVoid,
        string userVoid,
        SqlConnection? conn = null,
        SqlTransaction? tx = null)
    {
        const string sql = """
            UPDATE tb_trs_dobill_umum2
            SET
                fb_void = 1,
                UPDTGL = @tglVoid,
                UPDJAM = @jamVoid,
                UPDUSR = @userVoid
            WHERE
                fs_kd_trs = @penjualanId
            """;

        var dp = new { penjualanId, tglVoid, jamVoid, userVoid };
        ExecuteSql(sql, dp, conn, tx);
    }

    public void Delete(IPenjualanKey key, SqlConnection? conn = null, SqlTransaction? tx = null)
    {
        const string sql = """
            DELETE FROM tb_trs_dobill_umum2
            WHERE fs_kd_trs = @PenjualanId
            """;

        var dp = new { key.PenjualanId };
        ExecuteSql(sql, dp, conn, tx);
    }

    public IEnumerable<PenjualanItemDto> ListData(IPenjualanKey filter, SqlConnection? conn = null, SqlTransaction? tx = null)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs,
                aa.fs_kd_trs2,
                aa.fn_no_urut,
                aa.fb_void,
                aa.fs_kd_barang,
                aa.fs_nm_barang,
                aa.fs_kd_tipe_barang,
                aa.fb_racik,
                aa.fb_komponen,
                aa.fs_kd_racik,
                aa.fn_qty_racik,
                aa.fs_racik_qty,
                aa.fn_qty_barang,
                aa.fs_nm_satuan,
                aa.fn_harga_satuan,
                aa.fn_diskon,
                aa.fn_biaya,
                aa.fn_sub_total,
                aa.fn_tax_prosen,
                aa.fn_tax_rupiah,
                aa.fn_biaya_fee,
                aa.fn_total,
                aa.fs_etiket,
                aa.fb_asuransi,
                aa.fs_kd_tipe_jaminan,
                aa.fn_bulat,
                aa.fs_kd_satuan,
                aa.fn_etiket_qty,
                aa.fs_etiket_qty,
                aa.fn_etiket_hari,
                aa.fs_etiket_kd_pakai,
                aa.fs_etiket_catatan,
                aa.fs_etiket_jenis_obat,
                aa.fs_etiket_kd_satuan_pakai,
                aa.fd_etiket_cetak,
                aa.fb_belibebas,
                aa.fs_etiket_hari,
                aa.fb_paket_tarif,
                aa.fn_nilai_klaim,
                aa.fs_waktu_odd,
                aa.fn_etiket_cetak,
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
                aa.fn_etiket_sms,
                aa.fs_etiket_description,
                aa.fb_biaya_tax
            FROM
                tb_trs_dobill_umum2 aa
            WHERE
                aa.fs_kd_trs = @PenjualanId
            ORDER BY
                aa.fn_no_urut
            """;

        var dp = new { filter.PenjualanId };
        if (conn != null)
        {
            return conn.Query<PenjualanItemDto>(sql, dp, tx);
        }
        else
        {
            using var localConn = new SqlConnection(ConnStringHelper.Get(_opt));
            return localConn.Query<PenjualanItemDto>(sql, dp);
        }
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
}
