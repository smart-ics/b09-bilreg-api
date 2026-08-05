using Dapper;
using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public interface IFormulariumDal :
    IInsert<FormulariumDto>,
    IUpdate<FormulariumDto>,
    IDelete<IFormulariumKey>,
    IGetData<FormulariumDto, IFormulariumKey>,
    IListData<FormulariumDto>
{
}

public class FormulariumDal : IFormulariumDal
{
    private readonly DatabaseOptions _opt;

    public FormulariumDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(FormulariumDto dto)
    {
        const string sql = """
            INSERT INTO tb_formularium (
                fs_kd_formularium,
                fs_nm_formularium
            ) VALUES (
                @fs_kd_formularium,
                @fs_nm_formularium
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_formularium", dto.fs_kd_formularium, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_formularium", dto.fs_nm_formularium, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(FormulariumDto dto)
    {
        const string sql = """
            UPDATE tb_formularium
            SET
                fs_nm_formularium = @fs_nm_formularium
            WHERE
                fs_kd_formularium = @fs_kd_formularium
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_formularium", dto.fs_kd_formularium, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_formularium", dto.fs_nm_formularium, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IFormulariumKey key)
    {
        const string sql = """
            DELETE FROM tb_formularium
            WHERE
                fs_kd_formularium = @fs_kd_formularium
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_formularium", key.FormulariumId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public FormulariumDto GetData(IFormulariumKey key)
    {
        const string sql = """
            SELECT
                fs_kd_formularium,
                fs_nm_formularium
            FROM
                tb_formularium
            WHERE
                fs_kd_formularium = @fs_kd_formularium
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_formularium", key.FormulariumId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<FormulariumDto>(sql, dp);
    }

    public IEnumerable<FormulariumDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_formularium,
                fs_nm_formularium
            FROM
                tb_formularium
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<FormulariumDto>(sql);
    }
}