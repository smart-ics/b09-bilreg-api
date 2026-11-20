using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dals;

public interface IStockOpnameCountedItemDal :
    IInsert<StockOpnameCountedItemDto>,
    IUpdate<StockOpnameCountedItemDto>
{
    void Insert(IEnumerable<StockOpnameCountedItemDto> dtos);
    void Delete(string sessionId);
    IEnumerable<StockOpnameCountedItemDto> ListBySession(string sessionId);
    bool HasTupleBeenCountedByUser(string sessionId, string brgId, string batchNo, string expiredDate, string userId);
}

public class StockOpnameCountedItemDal : IStockOpnameCountedItemDal
{
    private const string SelectColumns = """
        CountedItemId, SessionId, BrgId, BatchNo, ExpiredDate,
        BaseSatuanId, TotalCountQty, LastCountTime
        """;

    private readonly DatabaseOptions _opt;

    public StockOpnameCountedItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(StockOpnameCountedItemDto dto)
    {
        const string sql = $"""
            INSERT INTO FARPU_StockOpnameCountedItem (
                {SelectColumns}
            ) VALUES (
                @CountedItemId, @SessionId, @BrgId, @BatchNo, @ExpiredDate,
                @BaseSatuanId, @TotalCountQty, @LastCountTime
            )
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Insert(IEnumerable<StockOpnameCountedItemDto> dtos)
    {
        const string sql = $"""
            INSERT INTO FARPU_StockOpnameCountedItem (
                {SelectColumns}
            ) VALUES (
                @CountedItemId, @SessionId, @BrgId, @BatchNo, @ExpiredDate,
                @BaseSatuanId, @TotalCountQty, @LastCountTime
            )
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        foreach (var dto in dtos)
        {
            conn.Execute(sql, BuildParams(dto));
        }
    }

    public void Update(StockOpnameCountedItemDto dto)
    {
        const string sql = """
            UPDATE FARPU_StockOpnameCountedItem SET
                TotalCountQty = @TotalCountQty,
                LastCountTime = @LastCountTime,
                BaseSatuanId = @BaseSatuanId
            WHERE CountedItemId = @CountedItemId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Delete(string sessionId)
    {
        const string sql = """
            DELETE FROM FARPU_StockOpnameCountedItem
            WHERE SessionId = @SessionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<StockOpnameCountedItemDto> ListBySession(string sessionId)
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameCountedItem WITH (NOLOCK)
            WHERE SessionId = @SessionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StockOpnameCountedItemDto>(sql, dp);
    }

    public bool HasTupleBeenCountedByUser(
        string sessionId,
        string brgId,
        string batchNo,
        string expiredDate,
        string userId)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM FARPU_StockOpnameCountEntry WITH (NOLOCK)
            WHERE SessionId = @SessionId
              AND BrgId = @BrgId
              AND BatchNo = @BatchNo
              AND ExpiredDate = @ExpiredDate
              AND CountedBy = @UserId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        dp.AddParam("@BatchNo", batchNo, SqlDbType.VarChar);
        dp.AddParam("@ExpiredDate", expiredDate, SqlDbType.VarChar);
        dp.AddParam("@UserId", userId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql, dp) > 0;
    }

    private static DynamicParameters BuildParams(StockOpnameCountedItemDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@CountedItemId", dto.CountedItemId, SqlDbType.VarChar);
        dp.AddParam("@SessionId", dto.SessionId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@BatchNo", dto.BatchNo, SqlDbType.VarChar);
        dp.AddParam("@ExpiredDate", dto.ExpiredDate, SqlDbType.VarChar);
        dp.AddParam("@BaseSatuanId", dto.BaseSatuanId, SqlDbType.VarChar);
        dp.AddParam("@TotalCountQty", dto.TotalCountQty, SqlDbType.Decimal);
        dp.AddParam("@LastCountTime", dto.LastCountTime, SqlDbType.DateTime);
        return dp;
    }
}
