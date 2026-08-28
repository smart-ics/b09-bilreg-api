using Bilreg.Application.SalesContext.ReturJualFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using PdfSharp.Pdf.Filters;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.SalesContext.ReturJualFeature;

public interface IReturJualItemDal :
    IInsertBulk<ReturJualItemDto>,
    IDelete<IReturJualKey>,
    IListData<ReturJualItemDto, IReturJualKey>
{
    IEnumerable<ReturJualItemQtyDto> ListQtyReturByPenjualan(
        IPenjualanKey jualKey, IReturJualKey returKey);
}

public class ReturJualItemDal : IReturJualItemDal
{
    private readonly DatabaseOptions _opt;

    public ReturJualItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<ReturJualItemDto> listModel)
    {
        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("ReturJualId", "fs_kd_trs");
        bcp.AddMap("ReturJualItemId", "fs_kd_trs2");
        bcp.AddMap("NoUrut", "fn_no_urut");
        bcp.AddMap("IsVoided", "fb_void");
        bcp.AddMap("BrgId", "fs_kd_barang");
        bcp.AddMap("QtyJual", "fn_qty_jual");
        bcp.AddMap("QtyRetur", "fn_qty_retur");
        bcp.AddMap("SatuanId", "fs_kd_satuan");
        bcp.AddMap("HargaJual", "fn_harga_jual");
        bcp.AddMap("HargaRetur", "fn_harga_retur");
        bcp.AddMap("TaxPerUnit", "fn_tax");
        bcp.AddMap("SubTotalJual", "fn_sub_total_jual");
        bcp.AddMap("SubTotalTax", "fn_sub_total_tax");
        bcp.AddMap("SubTotalRetur", "fn_sub_total_retur");
        bcp.AddMap("Total", "fn_total");

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "dbo.tb_trs_rjual_umum2";
        bcp.WriteToServer(fetched.AsDataTable());
    }
    public void Delete(IReturJualKey key)
    {
        const string sql = """
            DELETE FROM tb_trs_rjual_umum2
            WHERE fs_kd_trs = @ReturJualId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ReturJualId", key.ReturJualId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<ReturJualItemDto> ListData(IReturJualKey filter)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs AS PenjualanId,
                aa.fs_kd_trs2 AS PenjualanItemId,
                CAST(aa.fn_no_urut AS INT) AS NoUrut,
                aa.fb_void AS IsVoided,
                aa.fs_kd_barang AS BrgId,
                aa.fn_qty_jual AS QtyJual,
                aa.fn_qty_retur AS QtyRetur,    
                aa.fs_kd_satuan AS SatuanId,
                aa.fn_harga_jual AS HargaJual,
                aa.fn_harga_retur AS HargaRetur,
                aa.fn_tax AS TaxPerUnit,
                aa.fn_sub_total_jual AS SubTotalJual,
                aa.fn_sub_total_retur AS SubTotalRetur,
                aa.fn_sub_total_tax AS SubTotalTax,
                aa.fn_total AS Total,
                ISNULL(bb.fs_nm_barang, '') BarangName,
            	ISNULL(cc.fs_nm_satuan, '') SatuanName
            FROM
                tb_trs_rjual_umum2 aa
            	LEFT JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
                LEFT JOIN tb_satuan cc ON aa.fs_kd_satuan = cc.fs_kd_satuan
            WHERE
                aa.fs_kd_trs = @ReturJualId
            ORDER BY
                aa.fn_no_urut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ReturJualId", filter.ReturJualId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ReturJualItemDto>(sql, dp);
    }

    public IEnumerable<ReturJualItemQtyDto> ListQtyReturByPenjualan(
        IPenjualanKey jualKey, IReturJualKey returKey)
    {
        const string sql = """
            SELECT
                aa.fs_kd_barang AS BrgId,
                SUM(aa.fn_qty_retur) AS QtyRetur,
            	aa.fs_kd_satuan AS SatuanId
            FROM 
            	tb_trs_rjual_umum2 aa
            	INNER JOIN tb_trs_rjual_umum bb ON aa.fs_kd_trs = bb.fs_kd_trs
            WHERE 
            	bb.fs_kd_dobill_umum = @PenjualanId
                AND aa.fs_kd_trs <> @ReturJualId
                AND bb.fd_tgl_void = '3000-01-01'  AND aa.fb_void = 0
            GROUP BY aa.fs_kd_barang, aa.fs_kd_satuan
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PenjualanId", jualKey.PenjualanId, SqlDbType.VarChar);
        dp.AddParam("@ReturJualId", returKey.ReturJualId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ReturJualItemQtyDto>(sql, dp);
    }
}
