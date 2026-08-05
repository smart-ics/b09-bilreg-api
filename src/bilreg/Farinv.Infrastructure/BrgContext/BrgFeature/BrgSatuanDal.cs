using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public interface IBrgSatuanDal:
    IInsertBulk<BrgSatuanDto>,
    IDelete<IBrgKey>,
    IListData<BrgSatuanDto, IBrgKey>
{
}

public class BrgSatuanDal : IBrgSatuanDal
{
    private readonly DatabaseOptions _opt;
    public BrgSatuanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(IEnumerable<BrgSatuanDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        conn.Open();
        bcp.AddMap("fs_kd_barang", "fs_kd_barang");
        bcp.AddMap("fs_kd_satuan", "fs_kd_satuan");
        bcp.AddMap("fn_nilai", "fn_nilai");
        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "tb_barang_satuan";
        bcp.WriteToServer(fetched.AsDataTable());
    }
    
    public void Delete(IBrgKey key)
    {
        const string sql = """
           DELETE FROM
               tb_barang_satuan
           WHERE
               fs_kd_barang = @fs_kd_barang
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", key.BrgId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    
    }
    
    public IEnumerable<BrgSatuanDto> ListData(IBrgKey filter)
    {
        const string sql = """
           SELECT 
               aa.fs_kd_barang, aa.fs_kd_satuan, aa.fn_nilai,
               ISNULL(bb.fs_nm_satuan, '') AS fs_nm_satuan
           FROM 
               tb_barang_satuan aa
               LEFT JOIN tb_satuan bb ON aa.fs_kd_satuan = bb.fs_kd_satuan
           WHERE
               aa.fs_kd_barang = @fs_kd_barang
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", filter.BrgId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BrgSatuanDto>(sql, dp);
    }
}