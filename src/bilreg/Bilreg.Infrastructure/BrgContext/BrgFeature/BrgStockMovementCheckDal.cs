using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BrgContext.BrgFeature;

public class BrgStockMovementCheckDal : IBrgStockMovementCheck
{
    private readonly DatabaseOptions _opt;

    public BrgStockMovementCheckDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public bool HasStockMovementOrBalance(string brgId)
    {
        const string sql = """
            SELECT CASE 
                WHEN EXISTS (SELECT 1 FROM BILRG_StokMutasi WHERE BrgId = @BrgId) THEN 1
                WHEN EXISTS (SELECT 1 FROM BILRG_StokLokasi WHERE BrgId = @BrgId AND QtySisa > 0) THEN 1
                WHEN EXISTS (SELECT 1 FROM tb_stok WHERE fs_kd_barang = @BrgId AND fn_qty > 0) THEN 1
                ELSE 0 
            END
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql, dp) == 1;
    }
}
