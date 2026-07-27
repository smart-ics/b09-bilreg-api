using Dapper;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public interface ISatuanDal :
    IInsert<SatuanDto>,
    IUpdate<SatuanDto>,
    IDelete<ISatuanKey>,
    IGetData<SatuanDto, ISatuanKey>,
    IListData<SatuanDto>
{
}

public class SatuanDal : ISatuanDal
{
    private readonly DatabaseOptions _opt;

    public SatuanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(SatuanDto dto)
    {
        const string sql = """
            INSERT INTO tb_satuan (
                fs_kd_satuan,
                fs_nm_satuan
            ) VALUES (
                @fs_kd_satuan,
                @fs_nm_satuan
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_satuan", dto.fs_kd_satuan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_satuan", dto.fs_nm_satuan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(SatuanDto dto)
    {
        const string sql = """
            UPDATE tb_satuan
            SET
                fs_nm_satuan = @fs_nm_satuan
            WHERE
                fs_kd_satuan = @fs_kd_satuan
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_satuan", dto.fs_kd_satuan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_satuan", dto.fs_nm_satuan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ISatuanKey key)
    {
        const string sql = """
            DELETE FROM tb_satuan
            WHERE fs_kd_satuan = @fs_kd_satuan
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_satuan", key.SatuanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public SatuanDto GetData(ISatuanKey key)
    {
        const string sql = """
            SELECT
                fs_kd_satuan,
                fs_nm_satuan
            FROM tb_satuan
            WHERE fs_kd_satuan = @fs_kd_satuan
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_satuan", key.SatuanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<SatuanDto>(sql, dp);
    }

    public IEnumerable<SatuanDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_satuan,
                fs_nm_satuan
            FROM tb_satuan
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<SatuanDto>(sql);
    }
}