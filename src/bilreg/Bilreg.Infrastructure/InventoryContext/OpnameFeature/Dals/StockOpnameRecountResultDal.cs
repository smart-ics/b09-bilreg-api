using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dals;

public interface IStockOpnameRecountResultDal :
    IInsert<StockOpnameRecountResultDto>,
    IUpdate<StockOpnameRecountResultDto>
{
    void Delete(string recountResultId);
    void DeleteByTuple(string sessionId, string brgId, string batchNo, string expiredDate);
    StockOpnameRecountResultDto? GetData(string recountResultId);
    StockOpnameRecountResultDto? GetByTuple(string sessionId, string brgId, string batchNo, string expiredDate);
    IEnumerable<StockOpnameRecountResultDto> ListBySession(string sessionId);

    // Transition support for Repo / tests prior to P2-S03
    [Obsolete("Use DeleteByTuple")]
    void DeleteByVarianceItem(string varianceItemId);
    [Obsolete("Use GetByTuple")]
    StockOpnameRecountResultDto? GetByVarianceItem(string varianceItemId);
    [Obsolete("Use ListBySession or tuple query")]
    IEnumerable<StockOpnameRecountResultDto> ListByVarianceItems(IEnumerable<string> varianceItemIds);
}

public class StockOpnameRecountResultDal : IStockOpnameRecountResultDal
{
    private const string SelectColumns = """
        RecountResultId, SessionId, BrgId, BatchNo, ExpiredDate,
        RecountQty, RecountTimestamp, SystemQtyAtRecount, ResidualVarianceQty,
        DiscrepancyReason, SupervisorNotes, SupervisorUserId
        """;

    private readonly DatabaseOptions _opt;

    public StockOpnameRecountResultDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(StockOpnameRecountResultDto dto)
    {
        const string sql = $"""
            INSERT INTO FARPU_StockOpnameRecountResult (
                {SelectColumns}
            ) VALUES (
                @RecountResultId, @SessionId, @BrgId, @BatchNo, @ExpiredDate,
                @RecountQty, @RecountTimestamp, @SystemQtyAtRecount, @ResidualVarianceQty,
                @DiscrepancyReason, @SupervisorNotes, @SupervisorUserId
            )
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(StockOpnameRecountResultDto dto)
    {
        const string sql = """
            UPDATE FARPU_StockOpnameRecountResult SET
                RecountQty = @RecountQty,
                RecountTimestamp = @RecountTimestamp,
                SystemQtyAtRecount = @SystemQtyAtRecount,
                ResidualVarianceQty = @ResidualVarianceQty,
                DiscrepancyReason = @DiscrepancyReason,
                SupervisorNotes = @SupervisorNotes,
                SupervisorUserId = @SupervisorUserId
            WHERE RecountResultId = @RecountResultId
               OR (SessionId = @SessionId AND BrgId = @BrgId AND BatchNo = @BatchNo AND ExpiredDate = @ExpiredDate)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Delete(string recountResultId)
    {
        const string sql = """
            DELETE FROM FARPU_StockOpnameRecountResult
            WHERE RecountResultId = @RecountResultId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RecountResultId", recountResultId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void DeleteByTuple(string sessionId, string brgId, string batchNo, string expiredDate)
    {
        const string sql = """
            DELETE FROM FARPU_StockOpnameRecountResult
            WHERE SessionId = @SessionId
              AND BrgId = @BrgId
              AND BatchNo = @BatchNo
              AND ExpiredDate = @ExpiredDate
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        dp.AddParam("@BatchNo", batchNo, SqlDbType.VarChar);
        dp.AddParam("@ExpiredDate", expiredDate, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    [Obsolete("Use DeleteByTuple")]
    public void DeleteByVarianceItem(string varianceItemId)
    {
        var parts = varianceItemId.Split('_');
        if (parts.Length >= 4)
        {
            DeleteByTuple(parts[0], parts[1], parts[2], parts[3]);
        }
    }

    public StockOpnameRecountResultDto? GetData(string recountResultId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameRecountResult WITH (NOLOCK)
            WHERE RecountResultId = @RecountResultId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RecountResultId", recountResultId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockOpnameRecountResultDto>(sql, dp);
    }

    public StockOpnameRecountResultDto? GetByTuple(string sessionId, string brgId, string batchNo, string expiredDate)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameRecountResult WITH (NOLOCK)
            WHERE SessionId = @SessionId
              AND BrgId = @BrgId
              AND BatchNo = @BatchNo
              AND ExpiredDate = @ExpiredDate
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        dp.AddParam("@BatchNo", batchNo, SqlDbType.VarChar);
        dp.AddParam("@ExpiredDate", expiredDate, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockOpnameRecountResultDto>(sql, dp);
    }

    [Obsolete("Use GetByTuple")]
    public StockOpnameRecountResultDto? GetByVarianceItem(string varianceItemId)
    {
        var parts = varianceItemId.Split('_');
        if (parts.Length >= 4)
        {
            return GetByTuple(parts[0], parts[1], parts[2], parts[3]);
        }
        return GetData(varianceItemId);
    }

    public IEnumerable<StockOpnameRecountResultDto> ListBySession(string sessionId)
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameRecountResult WITH (NOLOCK)
            WHERE SessionId = @SessionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StockOpnameRecountResultDto>(sql, dp);
    }

    [Obsolete("Use ListBySession or tuple query")]
    public IEnumerable<StockOpnameRecountResultDto> ListByVarianceItems(IEnumerable<string> varianceItemIds)
    {
        var idList = varianceItemIds?.ToList() ?? [];
        if (idList.Count == 0)
            return [];

        var first = idList.FirstOrDefault(x => x.Contains('_'));
        if (first != null)
        {
            var sessionId = first.Split('_')[0];
            return ListBySession(sessionId);
        }
        return [];
    }

    internal static DynamicParameters BuildParams(StockOpnameRecountResultDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@RecountResultId", dto.RecountResultId, SqlDbType.VarChar);
        dp.AddParam("@SessionId", dto.SessionId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@BatchNo", dto.BatchNo, SqlDbType.VarChar);
        dp.AddParam("@ExpiredDate", dto.ExpiredDate, SqlDbType.VarChar);
        dp.AddParam("@RecountQty", dto.RecountQty, SqlDbType.Decimal);
        dp.AddParam("@RecountTimestamp", dto.RecountTimestamp, SqlDbType.DateTime);
        dp.AddParam("@SystemQtyAtRecount", dto.SystemQtyAtRecount, SqlDbType.Decimal);
        dp.AddParam("@ResidualVarianceQty", dto.ResidualVarianceQty, SqlDbType.Decimal);
        dp.AddParam("@DiscrepancyReason", dto.DiscrepancyReason ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@SupervisorNotes", dto.SupervisorNotes ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@SupervisorUserId", dto.SupervisorUserId ?? string.Empty, SqlDbType.VarChar);
        return dp;
    }
}
