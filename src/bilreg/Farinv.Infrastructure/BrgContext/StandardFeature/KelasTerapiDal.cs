using Dapper;
using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public interface IKelasTerapiDal :
    IInsert<KelasTerapiDto>,
    IUpdate<KelasTerapiDto>,
    IDelete<IKelasTerapiKey>,
    IGetData<KelasTerapiDto, IKelasTerapiKey>,
    IListData<KelasTerapiDto>
{
}

public class KelasTerapiDal : IKelasTerapiDal
{
    private readonly DatabaseOptions _opt;

    public KelasTerapiDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KelasTerapiDto dto)
    {
        const string sql = """
            INSERT INTO tb_kelas_terapi (
                fs_kd_kelas_terapi,
                fs_nm_kelas_terapi
            ) VALUES (
                @fs_kd_kelas_terapi,
                @fs_nm_kelas_terapi
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas_terapi", dto.fs_kd_kelas_terapi, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelas_terapi", dto.fs_nm_kelas_terapi, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KelasTerapiDto dto)
    {
        const string sql = """
            UPDATE tb_kelas_terapi
            SET
                fs_nm_kelas_terapi = @fs_nm_kelas_terapi
            WHERE
                fs_kd_kelas_terapi = @fs_kd_kelas_terapi
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas_terapi", dto.fs_kd_kelas_terapi, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelas_terapi", dto.fs_nm_kelas_terapi, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKelasTerapiKey key)
    {
        const string sql = """
            DELETE FROM tb_kelas_terapi
            WHERE fs_kd_kelas_terapi = @fs_kd_kelas_terapi
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas_terapi", key.KelasTerapiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KelasTerapiDto GetData(IKelasTerapiKey key)
    {
        const string sql = """
            SELECT
                fs_kd_kelas_terapi,
                fs_nm_kelas_terapi
            FROM tb_kelas_terapi
            WHERE fs_kd_kelas_terapi = @fs_kd_kelas_terapi
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas_terapi", key.KelasTerapiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<KelasTerapiDto>(sql, dp);
    }

    public IEnumerable<KelasTerapiDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_kelas_terapi,
                fs_nm_kelas_terapi
            FROM tb_kelas_terapi
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KelasTerapiDto>(sql);
    }
}
