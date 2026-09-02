using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PurchaseContext.PurchaseOrderFeature;

public interface IPurchaseOrderItemDal :
    IInsertBulk<PurchaseOrderItemDto>,
    IDelete<IPurchaseOrderKey>,
    IListData<PurchaseOrderItemDto, IPurchaseOrderKey>;

public class PurchaseOrderItemDal: IPurchaseOrderItemDal
{
    private readonly DatabaseOptions _opt;

    public PurchaseOrderItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(IEnumerable<PurchaseOrderItemDto> listModel)
    {
        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("PurchaseOrderId", "fs_kd_trs");
        bcp.AddMap("NoUrut", "fn_no_urut");
        bcp.AddMap("BrgId", "fs_kd_barang");
        bcp.AddMap("SatuanId", "fs_kd_satuan");
        bcp.AddMap("Harga", "fn_harga");
        bcp.AddMap("QtyStok", "fn_qty_stok");
        bcp.AddMap("Qty", "fn_qty");
        bcp.AddMap("Subtotal", "fn_sub_total");
        bcp.AddMap("DiskonPercentage", "fn_diskon_prosen");
        bcp.AddMap("DiskonTotal", "fn_diskon");
        bcp.AddMap("TaxPercentage", "fn_tax_prosen");
        bcp.AddMap("TaxTotal", "fn_tax_rupiah");
        bcp.AddMap("BiayaLain", "fn_biaya_lain");
        bcp.AddMap("Total", "fn_total");

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "dbo.tb_trs_po2";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPurchaseOrderKey key)
    {
        const string sql = """
           DELETE FROM tb_trs_po2
           WHERE fs_kd_trs = @PurchaseOrderId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@PurchaseOrderId", key.PurchaseOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PurchaseOrderItemDto> ListData(IPurchaseOrderKey filter)
    {
        const string sql = """
           SELECT
               aa.fs_kd_trs AS PurchaseOrderId,
               aa.fn_no_urut AS NoUrut,
               aa.fs_kd_barang AS BrgId,
               aa.fs_kd_satuan AS SatuanId,
               aa.fn_harga AS Harga,
               aa.fn_qty_stok AS QtyStok,
               aa.fn_qty AS Qty,
               aa.fn_sub_total AS Subtotal,
               aa.fn_diskon_prosen AS DiskonPercentage,
               aa.fn_diskon AS DiskonTotal,
               aa.fn_tax_prosen AS TaxPercentage,
               aa.fn_tax_rupiah AS TaxTotal,
               aa.fn_biaya_lain AS BiayaLain,
               aa.fn_total AS Total,
               ISNULL(bb.fs_nm_barang, '') AS BrgName,
               ISNULL(cc.fs_nm_satuan, '') AS SatuanName
           FROM
               tb_trs_po2 aa
               LEFT JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
               LEFT JOIN tb_satuan cc ON aa.fs_kd_satuan = cc.fs_kd_satuan
           WHERE
               aa.fs_kd_trs = @PurchaseOrderId
           ORDER BY
               aa.fn_no_urut
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@PurchaseOrderId", filter.PurchaseOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PurchaseOrderItemDto>(sql, dp);
    }
}