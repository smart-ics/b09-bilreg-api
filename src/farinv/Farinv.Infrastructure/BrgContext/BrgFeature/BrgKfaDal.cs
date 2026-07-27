using Dapper;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public interface IBrgKfaDal :
    IInsert<BrgKfaDto>,
    IUpdate<BrgKfaDto>,
    IDelete<IBrgKey>,
    IGetData<BrgKfaDto, IBrgKey>,
    IListData<BrgKfaDto>
{
}

public class BrgKfaDal : IBrgKfaDal
{
    private readonly DatabaseOptions _opt;

    public BrgKfaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BrgKfaDto dto)
    {
        const string sql = """
            INSERT INTO FARPU_MapBrgKfa (
                BrgId,
                KfaId,
                KfaName
            ) VALUES (
                @BrgId,
                @KfaId,
                @KfaName
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@KfaId", dto.KfaId, SqlDbType.VarChar);
        dp.AddParam("@KfaName", dto.KfaName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BrgKfaDto dto)
    {
        const string sql = """
            UPDATE FARPU_MapBrgKfa
            SET
                KfaName = @KfaName
            WHERE
                BrgId = @BrgId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@KfaName", dto.KfaName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBrgKey key)
    {
        const string sql = """
            DELETE FROM FARPU_MapBrgKfa
            WHERE BrgId = @BrgId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", key.BrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BrgKfaDto GetData(IBrgKey key)
    {
        const string sql = """
            SELECT
                BrgId,
                KfaId,
                KfaName
            FROM FARPU_MapBrgKfa
            WHERE BrgId = @BrgId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", key.BrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BrgKfaDto>(sql, dp);
    }

    public IEnumerable<BrgKfaDto> ListData()
    {
        const string sql = """
            SELECT
                BrgId,
                KfaId,
                KfaName
            FROM FARPU_MapBrgKfa
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BrgKfaDto>(sql);
    }
}
