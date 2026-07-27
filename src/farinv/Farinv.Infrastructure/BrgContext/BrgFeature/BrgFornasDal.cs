using Dapper;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public interface IBrgFornasDal :
    IInsert<BrgFornasDto>,
    IUpdate<BrgFornasDto>,
    IDelete<IBrgKey>,
    IGetData<BrgFornasDto, IBrgKey>,
    IListData<BrgFornasDto>
{
}

public class BrgFornasDal : IBrgFornasDal
{
    private readonly DatabaseOptions _opt;

    public BrgFornasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BrgFornasDto dto)
    {
        const string sql = """
            INSERT INTO tb_barang_fornas (
                fs_kd_barang,
                FornasId
            ) VALUES (
                @fs_kd_barang,
                @FornasId
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", dto.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@FornasId", dto.FornasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BrgFornasDto dto)
    {
        const string sql = """
            UPDATE tb_barang_fornas
            SET
                FornasId = @FornasId
            WHERE
                fs_kd_barang = @fs_kd_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", dto.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@FornasId", dto.FornasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBrgKey key)
    {
        const string sql = """
            DELETE FROM tb_barang_fornas
            WHERE fs_kd_barang = @fs_kd_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", key.BrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BrgFornasDto GetData(IBrgKey key)
    {
        const string sql = """
            SELECT
                fs_kd_barang,
                FornasId
            FROM tb_barang_fornas
            WHERE fs_kd_barang = @fs_kd_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", key.BrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BrgFornasDto>(sql, dp);
    }

    public IEnumerable<BrgFornasDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_barang,
                FornasId
            FROM tb_barang_fornas
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BrgFornasDto>(sql);
    }
}
