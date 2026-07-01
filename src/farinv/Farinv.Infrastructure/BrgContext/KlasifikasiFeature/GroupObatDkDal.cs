using Dapper;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public interface IGroupObatDkDal :
    IInsert<GroupObatDkDto>,
    IUpdate<GroupObatDkDto>,
    IDelete<IGroupObatDkKey>,
    IGetData<GroupObatDkDto, IGroupObatDkKey>,
    IListData<GroupObatDkDto>
{
}

public class GroupObatDkDal : IGroupObatDkDal
{
    private readonly DatabaseOptions _opt;

    public GroupObatDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(GroupObatDkDto dto)
    {
        const string sql = """
            INSERT INTO tb_gol_obat_dk (
                fs_kd_gol_obat_dk,
                fs_nm_gol_obat_dk
            ) VALUES (
                @fs_kd_gol_obat_dk,
                @fs_nm_gol_obat_dk
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_gol_obat_dk", dto.fs_kd_gol_obat_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_gol_obat_dk", dto.fs_nm_gol_obat_dk, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(GroupObatDkDto dto)
    {
        const string sql = """
            UPDATE tb_gol_obat_dk
            SET
                fs_nm_gol_obat_dk = @fs_nm_gol_obat_dk
            WHERE
                fs_kd_gol_obat_dk = @fs_kd_gol_obat_dk
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_gol_obat_dk", dto.fs_kd_gol_obat_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_gol_obat_dk", dto.fs_nm_gol_obat_dk, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGroupObatDkKey key)
    {
        const string sql = """
            DELETE FROM tb_gol_obat_dk
            WHERE fs_kd_gol_obat_dk = @fs_kd_gol_obat_dk
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_gol_obat_dk", key.GroupObatDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GroupObatDkDto GetData(IGroupObatDkKey key)
    {
        const string sql = """
            SELECT
                fs_kd_gol_obat_dk,
                fs_nm_gol_obat_dk
            FROM tb_gol_obat_dk
            WHERE fs_kd_gol_obat_dk = @fs_kd_gol_obat_dk
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_gol_obat_dk", key.GroupObatDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<GroupObatDkDto>(sql, dp);
    }

    public IEnumerable<GroupObatDkDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_gol_obat_dk,
                fs_nm_gol_obat_dk
            FROM tb_gol_obat_dk
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GroupObatDkDto>(sql);
    }
}
