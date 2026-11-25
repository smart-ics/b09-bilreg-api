using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dals;

public interface IStockOpnameScopeItemDal :
    IInsert<StockOpnameScopeItemDto>
{
    void Insert(IEnumerable<StockOpnameScopeItemDto> dtos);
    void Delete(string sessionId);
    IEnumerable<StockOpnameScopeItemDto> ListBySession(string sessionId);
}

public class StockOpnameScopeItemDal : IStockOpnameScopeItemDal
{
    private const string SelectColumns = """
        SessionId, BrgId
        """;

    private readonly DatabaseOptions _opt;

    public StockOpnameScopeItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(StockOpnameScopeItemDto dto)
    {
        const string sql = """
            INSERT INTO FARPU_StockOpnameScopeItem (
                SessionId, BrgId
            ) VALUES (
                @SessionId, @BrgId
            )
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Insert(IEnumerable<StockOpnameScopeItemDto> dtos)
    {
        const string sql = """
            INSERT INTO FARPU_StockOpnameScopeItem (
                SessionId, BrgId
            ) VALUES (
                @SessionId, @BrgId
            )
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        foreach (var dto in dtos)
        {
            conn.Execute(sql, BuildParams(dto));
        }
    }

    public void Delete(string sessionId)
    {
        const string sql = """
            DELETE FROM FARPU_StockOpnameScopeItem
            WHERE SessionId = @SessionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<StockOpnameScopeItemDto> ListBySession(string sessionId)
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameScopeItem WITH (NOLOCK)
            WHERE SessionId = @SessionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StockOpnameScopeItemDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(StockOpnameScopeItemDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", dto.SessionId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        return dp;
    }
}
