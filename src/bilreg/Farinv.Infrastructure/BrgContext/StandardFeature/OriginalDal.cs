using Dapper;
using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public interface IOriginalDal :
    IInsert<OriginalDto>,
    IUpdate<OriginalDto>,
    IDelete<IOriginalKey>,
    IGetData<OriginalDto, IOriginalKey>,
    IListData<OriginalDto>
{
}

public class OriginalDal : IOriginalDal
{
    private readonly DatabaseOptions _opt;

    public OriginalDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(OriginalDto dto)
    {
        const string sql = """
            INSERT INTO tb_original (
                fs_kd_original,
                fs_nm_original
            ) VALUES (
                @fs_kd_original,
                @fs_nm_original
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_original", dto.fs_kd_original, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_original", dto.fs_nm_original, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(OriginalDto dto)
    {
        const string sql = """
            UPDATE tb_original
            SET
                fs_nm_original = @fs_nm_original
            WHERE
                fs_kd_original = @fs_kd_original
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_original", dto.fs_kd_original, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_original", dto.fs_nm_original, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IOriginalKey key)
    {
        const string sql = """
            DELETE FROM tb_original
            WHERE fs_kd_original = @fs_kd_original
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_original", key.OriginalId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public OriginalDto GetData(IOriginalKey key)
    {
        const string sql = """
            SELECT
                fs_kd_original,
                fs_nm_original
            FROM tb_original
            WHERE fs_kd_original = @fs_kd_original
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_original", key.OriginalId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<OriginalDto>(sql, dp);
    }

    public IEnumerable<OriginalDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_original,
                fs_nm_original
            FROM tb_original
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OriginalDto>(sql);
    }
}
