using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public interface IStokDal :
    IListData<StokView, ILayananKey, string>
{
}

public class StokDal : IStokDal
{
    private readonly DatabaseOptions _opt;

    public StokDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<StokView> ListData(ILayananKey layanan, string searchKeyword)
    {
        const string sql = """
           SELECT
               aa.fs_kd_barang AS BrgId,
               bb.fs_nm_barang AS BrgName,
               SUM(aa.fn_qty) AS Qty,
               bb.fs_kd_sat_jual AS Satuan
           FROM 
               tb_stok aa
               INNER JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
           WHERE
               aa.fs_kd_layanan = @fs_kd_layanan
               AND bb.fs_nm_barang LIKE @fs_nm_barang
           GROUP BY
               aa.fs_kd_barang,
               bb.fs_nm_barang,
               bb.fs_kd_sat_jual
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", layanan.LayananId, SqlDbType.VarChar);
        dp.Add("@fs_nm_barang", $"%{searchKeyword}%");

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<StokView>(sql, dp);
        return result;
    }
}