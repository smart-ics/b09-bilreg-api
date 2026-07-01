using Dapper;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public interface IBentukDal :
    IInsert<BentukDto>,
    IUpdate<BentukDto>,
    IDelete<IBentukKey>,
    IGetData<BentukDto, IBentukKey>,
    IListData<BentukDto>
{
}

public class BentukDal : IBentukDal
{
    private readonly DatabaseOptions _opt;

    public BentukDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BentukDto dto)
    {
        const string sql = """
            INSERT INTO tb_bentuk (
                fs_kd_bentuk,
                fs_nm_bentuk
            ) VALUES (
                @fs_kd_bentuk,
                @fs_nm_bentuk
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bentuk", dto.fs_kd_bentuk, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_bentuk", dto.fs_nm_bentuk, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BentukDto dto)
    {
        const string sql = """
            UPDATE tb_bentuk
            SET
                fs_nm_bentuk = @fs_nm_bentuk
            WHERE
                fs_kd_bentuk = @fs_kd_bentuk
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bentuk", dto.fs_kd_bentuk, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_bentuk", dto.fs_nm_bentuk, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBentukKey key)
    {
        const string sql = """
            DELETE FROM tb_bentuk
            WHERE fs_kd_bentuk = @fs_kd_bentuk
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bentuk", key.BentukId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BentukDto GetData(IBentukKey key)
    {
        const string sql = """
            SELECT
                fs_kd_bentuk,
                fs_nm_bentuk
            FROM tb_bentuk
            WHERE fs_kd_bentuk = @fs_kd_bentuk
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bentuk", key.BentukId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BentukDto>(sql, dp);
    }

    public IEnumerable<BentukDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_bentuk,
                fs_nm_bentuk
            FROM tb_bentuk
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BentukDto>(sql);
    }
}
