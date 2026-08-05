using Dapper;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public interface ISifatDal :
    IInsert<SifatDto>,
    IUpdate<SifatDto>,
    IDelete<ISifatKey>,
    IGetData<SifatDto, ISifatKey>,
    IListData<SifatDto>
{
}

public class SifatDal : ISifatDal
{
    private readonly DatabaseOptions _opt;

    public SifatDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(SifatDto dto)
    {
        const string sql = """
            INSERT INTO tb_sifat (
                fs_kd_sifat,
                fs_nm_sifat
            ) VALUES (
                @fs_kd_sifat,
                @fs_nm_sifat
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sifat", dto.fs_kd_sifat, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_sifat", dto.fs_nm_sifat, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(SifatDto dto)
    {
        const string sql = """
            UPDATE tb_sifat
            SET
                fs_nm_sifat = @fs_nm_sifat
            WHERE
                fs_kd_sifat = @fs_kd_sifat
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sifat", dto.fs_kd_sifat, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_sifat", dto.fs_nm_sifat, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ISifatKey key)
    {
        const string sql = """
            DELETE FROM tb_sifat
            WHERE fs_kd_sifat = @fs_kd_sifat
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sifat", key.SifatId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public SifatDto GetData(ISifatKey key)
    {
        const string sql = """
            SELECT
                fs_kd_sifat,
                fs_nm_sifat
            FROM tb_sifat
            WHERE fs_kd_sifat = @fs_kd_sifat
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sifat", key.SifatId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<SifatDto>(sql, dp);
    }

    public IEnumerable<SifatDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_sifat,
                fs_nm_sifat
            FROM tb_sifat
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<SifatDto>(sql);
    }
}
