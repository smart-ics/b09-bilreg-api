using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public interface IPasienIdDal :
    IInsert<PasienIdDto>,
    IUpdate<PasienIdDto>,
    IDelete<IPasienKey>,
    IListData<PasienIdDto, IPasienKey>
{
}

public class PasienIdDal : IPasienIdDal
{
    private readonly DatabaseOptions _opt;

    public PasienIdDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PasienIdDto dto)
    {
        const string sql = """
            INSERT INTO tc_mr_id(
               fs_mr, JenisID, NoID)
            VALUES(
               @fs_mr, @JenisID, @NoID)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", dto.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@JenisID", dto.JenisID, SqlDbType.VarChar);
        dp.AddParam("@NoID", dto.NoID, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PasienIdDto dto)
    {
        const string sql = """
            UPDATE 
                tc_mr_id
            SET
               NoID = @NoID
            WHERE
               fs_mr = @fs_mr
               AND JenisID = @JenisID
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", dto.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@JenisID", dto.JenisID, SqlDbType.VarChar);
        dp.AddParam("@NoID", dto.NoID, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPasienKey key)
    {
        const string sql = """
            DELETE FROM 
                tc_mr_id
            WHERE
               fs_mr = @fs_mr
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PasienIdDto> ListData(IPasienKey key)
    {
        const string sql = """
            SELECT 
                fs_mr, JenisID, NoID
            FROM
                tc_mr_id
            WHERE
               fs_mr = @fs_mr
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PasienIdDto>(sql, dp);
    }
}
