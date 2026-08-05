using Dapper;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public interface IGolonganDal :
    IInsert<GolonganDto>,
    IUpdate<GolonganDto>,
    IDelete<IGolonganKey>,
    IGetData<GolonganDto, IGolonganKey>,
    IListData<GolonganDto>
{
}

public class GolonganDal : IGolonganDal
{
    private readonly DatabaseOptions _opt;

    public GolonganDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(GolonganDto dto)
    {
        const string sql = """
            INSERT INTO tb_golongan (
                fs_kd_golongan,
                fs_nm_golongan
            ) VALUES (
                @fs_kd_golongan,
                @fs_nm_golongan
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_golongan", dto.fs_kd_golongan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_golongan", dto.fs_nm_golongan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(GolonganDto dto)
    {
        const string sql = """
            UPDATE tb_golongan
            SET
                fs_nm_golongan = @fs_nm_golongan
            WHERE
                fs_kd_golongan = @fs_kd_golongan
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_golongan", dto.fs_kd_golongan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_golongan", dto.fs_nm_golongan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGolonganKey key)
    {
        const string sql = """
            DELETE FROM tb_golongan
            WHERE fs_kd_golongan = @fs_kd_golongan
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_golongan", key.GolonganId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GolonganDto GetData(IGolonganKey key)
    {
        const string sql = """
            SELECT
                fs_kd_golongan,
                fs_nm_golongan
            FROM tb_golongan
            WHERE fs_kd_golongan = @fs_kd_golongan
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_golongan", key.GolonganId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<GolonganDto>(sql, dp);
    }

    public IEnumerable<GolonganDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_golongan,
                fs_nm_golongan
            FROM tb_golongan
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GolonganDto>(sql);
    }
}