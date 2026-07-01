using Dapper;
using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public interface IGolTerapiDal :
    IInsert<GolTerapiDto>,
    IUpdate<GolTerapiDto>,
    IDelete<IGolTerapiKey>,
    IGetData<GolTerapiDto, IGolTerapiKey>,
    IListData<GolTerapiDto>
{
}

public class GolTerapiDal : IGolTerapiDal
{
    private readonly DatabaseOptions _opt;

    public GolTerapiDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(GolTerapiDto dto)
    {
        const string sql = """
            INSERT INTO tb_gol_terapi (
                fs_kd_gol_terapi,
                fs_nm_gol_terapi
            ) VALUES (
                @fs_kd_gol_terapi,
                @fs_nm_gol_terapi
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_gol_terapi", dto.fs_kd_gol_terapi, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_gol_terapi", dto.fs_nm_gol_terapi, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(GolTerapiDto dto)
    {
        const string sql = """
            UPDATE tb_gol_terapi
            SET
                fs_nm_gol_terapi = @fs_nm_gol_terapi
            WHERE
                fs_kd_gol_terapi = @fs_kd_gol_terapi
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_gol_terapi", dto.fs_kd_gol_terapi, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_gol_terapi", dto.fs_nm_gol_terapi, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGolTerapiKey key)
    {
        const string sql = """
            DELETE FROM tb_gol_terapi
            WHERE fs_kd_gol_terapi = @fs_kd_gol_terapi
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_gol_terapi", key.GolTerapiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GolTerapiDto GetData(IGolTerapiKey key)
    {
        const string sql = """
            SELECT
                fs_kd_gol_terapi,
                fs_nm_gol_terapi
            FROM tb_gol_terapi
            WHERE fs_kd_gol_terapi = @fs_kd_gol_terapi
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_gol_terapi", key.GolTerapiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<GolTerapiDto>(sql, dp);
    }

    public IEnumerable<GolTerapiDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_gol_terapi,
                fs_nm_gol_terapi
            FROM tb_gol_terapi
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GolTerapiDto>(sql);
    }
}
