using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.SalesContext.PenjualanFeature;

public interface IPenjualanItemBatchDal :
    IInsertBulk<PenjualanItemBatchDto>,
    IDelete<IPenjualanKey>,
    IListData<PenjualanItemBatchDto, IPenjualanKey>
{
}

public class PenjualanItemBatchDal : IPenjualanItemBatchDal
{
    private readonly DatabaseOptions _opt;

    public PenjualanItemBatchDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PenjualanItemBatchDto> listModel)
    {
        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("PenjualanId", "fs_kd_trs");
        bcp.AddMap("PenjualanItemId", "fs_kd_trs2");
        bcp.AddMap("PenjualanItemBatchId", "fs_kd_trs3");
        bcp.AddMap("NoUrut", "fn_no_urut");
        bcp.AddMap("IsVoided", "fb_void");
        bcp.AddMap("BrgId", "fs_kd_barang");
        bcp.AddMap("Qty", "fn_qty");
        bcp.AddMap("SatuanId", "fs_kd_satuan");
        bcp.AddMap("TglEd", "fd_tgl_ed");
        bcp.AddMap("NoBatch", "fs_no_batch");
        bcp.AddMap("PoId", "fs_kd_po");
        bcp.AddMap("DoId", "fs_kd_do");
        bcp.AddMap("Hpp", "fn_hpp");

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "dbo.tb_trs_dobill_umum3";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPenjualanKey key)
    {
        const string sql = """
            DELETE FROM tb_trs_dobill_umum3
            WHERE fs_kd_trs = @PenjualanId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PenjualanId", key.PenjualanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PenjualanItemBatchDto> ListData(IPenjualanKey filter)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs AS PenjualanId,
                aa.fs_kd_trs2 AS PenjualanItemId,
                aa.fs_kd_trs3 AS PenjualanItemBatchId,
                CAST(aa.fn_no_urut AS INT) AS NoUrut,
                aa.fb_void AS IsVoided,
                aa.fs_kd_barang AS BrgId,
                aa.fn_qty AS Qty,
                aa.fs_kd_satuan AS SatuanId,
                aa.fd_tgl_ed AS TglEd,
                aa.fs_no_batch AS NoBatch,
                aa.fs_kd_po AS PoId,
                aa.fs_kd_do AS DoId,
                aa.fn_hpp AS Hpp
            FROM
                tb_trs_dobill_umum3 aa
            WHERE
                aa.fs_kd_trs = @PenjualanId
            ORDER BY
                aa.fn_no_urut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PenjualanId", filter.PenjualanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PenjualanItemBatchDto>(sql, dp);
    }
}
