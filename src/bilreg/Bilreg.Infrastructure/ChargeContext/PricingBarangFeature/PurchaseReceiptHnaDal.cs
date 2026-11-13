using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

public interface IPurchaseReceiptHnaDal : IPurchaseReceiptHnaQuery
{
}

/// <summary>
/// Query pembacaan data perolehan HPP dari tabel legacy <c>tb_trs_do2</c> (baris) dan
/// <c>tb_trs_do</c> (header) sebagai basis komputasi varian HNA pengadaan.
/// Hanya baris non-void (<c>fb_void = 0</c>) dari dokumen non-void (<c>fd_tgl_void = '3000-01-01'</c>) yang dibaca.
/// </summary>
public class PurchaseReceiptHnaDal : IPurchaseReceiptHnaDal
{
    private const string EmptyVoidDate = "3000-01-01";

    private readonly DatabaseOptions _opt;

    public PurchaseReceiptHnaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<PurchaseReceiptHnaDto> GetProcurementHistory(
        string barangId, DateTime? start = null, DateTime? end = null)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs                AS ReceiptId,
                aa.fn_no_urut               AS NoUrut,
                aa.fs_kd_barang             AS BarangId,
                ISNULL(aa.fs_kd_satuan, '') AS SatuanId,
                ISNULL(aa.fn_konversi, 0)   AS Konversi,
                ISNULL(aa.fn_harga, 0)      AS Harga,
                ISNULL(aa.fn_diskon, 0)     AS Diskon,
                ISNULL(aa.fn_tax, 0)        AS Tax,
                ISNULL(aa.fn_qty, 0)        AS Qty,
                ISNULL(aa.fn_sub_total, 0)  AS SubTotal,
                ISNULL(aa.fn_total, 0)      AS Total,
                ISNULL(bb.fd_tgl_trs, '3000-01-01') AS TglTrs
            FROM tb_trs_do2 aa
            INNER JOIN tb_trs_do bb ON aa.fs_kd_trs = bb.fs_kd_trs
            WHERE aa.fs_kd_barang = @barangId
              AND aa.fb_void = 0
              AND bb.fd_tgl_void = '3000-01-01'
              AND bb.fd_tgl_trs >= @start
              AND bb.fd_tgl_trs <= @end
            ORDER BY bb.fd_tgl_trs, aa.fs_kd_trs, aa.fn_no_urut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@barangId", barangId, SqlDbType.VarChar);
        dp.AddParam("@start", HnaBatchDto.FormatDate(start ?? DateTime.MinValue), SqlDbType.VarChar);
        dp.AddParam("@end", HnaBatchDto.FormatDate(end ?? AppConst.DEF_DATE), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<PurchaseReceiptHnaRow>(sql, dp);
        return rows is null ? [] : rows.Select(x => x.ToDto()).ToList();
    }

    /// <summary>
    /// Harga beli per satuan kecil dasar: <c>fn_harga</c> dibagi koefisien konversi satuan beli
    /// (<c>fn_konversi</c>), konsisten dengan legacy <c>clbTrsHNA.GetHNAGenFromSPHDO</c>
    /// (<c>Round(fn_harga / koef, 2)</c>).
    /// </summary>
    private static decimal HargaBeliPerSatuanKecil(decimal harga, decimal konversi)
    {
        var koef = konversi > 1m ? konversi : 1m;
        return Math.Round(harga / koef, 2);
    }

    private static DateTime ParseDate(string value) =>
        DateTime.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var parsed)
            ? parsed
            : AppConst.DEF_DATE;

    private record PurchaseReceiptHnaRow(
        string ReceiptId,
        decimal NoUrut,
        string BarangId,
        string SatuanId,
        decimal Konversi,
        decimal Harga,
        decimal Diskon,
        decimal Tax,
        decimal Qty,
        decimal SubTotal,
        decimal Total,
        string TglTrs)
    {
        public PurchaseReceiptHnaDto ToDto() => new(
            ReceiptId,
            (int)NoUrut,
            BarangId,
            SatuanId,
            Konversi,
            Harga,
            Diskon,
            Tax,
            Qty,
            SubTotal,
            Total,
            ParseDate(TglTrs),
            HargaBeliPerSatuanKecil(Harga, Konversi));
    }
}
