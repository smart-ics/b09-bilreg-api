using Dapper;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public interface IBrgFormulariumDal :
    IInsert<BrgFormulariumDto>,
    IDelete<IBrgKey>,
    IListData<BrgFormulariumDto, IBrgKey>
{
}

public class BrgFormulariumDal : IBrgFormulariumDal
{
    private readonly DatabaseOptions _opt;

    public BrgFormulariumDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BrgFormulariumDto dto)
    {
        const string sql = """
            INSERT INTO tb_barang_formularium (
                fs_kd_barang,
                fs_kd_formularium
            ) VALUES (
                @fs_kd_barang,
                @fs_kd_formularium
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", dto.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_formularium", dto.fs_kd_formularium, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBrgKey key)
    {
        const string sql = """
            DELETE FROM tb_barang_formularium
            WHERE fs_kd_barang = @fs_kd_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", key.BrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<BrgFormulariumDto> ListData(IBrgKey filter)
    {
        const string sql = """
            SELECT
                fs_kd_barang,
                fs_kd_formularium
            FROM tb_barang_formularium
            WHERE fs_kd_barang = @fs_kd_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", filter.BrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BrgFormulariumDto>(sql, dp);
    }
}