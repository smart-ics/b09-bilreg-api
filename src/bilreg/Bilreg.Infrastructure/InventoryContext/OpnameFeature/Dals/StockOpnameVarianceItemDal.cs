using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dals;

public interface IStockOpnameVarianceItemDal :
    IInsert<StockOpnameVarianceItemDto>,
    IUpdate<StockOpnameVarianceItemDto>
{
    void Insert(IEnumerable<StockOpnameVarianceItemDto> dtos);
    void UpdateStatus(string sessionId, string brgId, string batchNo, string expiredDate, string status, decimal? finalQuantity = null);
    void Delete(string sessionId);
    StockOpnameVarianceItemDto? GetData(string sessionId, string brgId, string batchNo, string expiredDate);
    IEnumerable<StockOpnameVarianceItemDto> ListBySession(string sessionId);

    // Transition support for Repo / tests prior to P2-S03
    [Obsolete("Use UpdateStatus with composite tuple")]
    void UpdateState(string varianceItemId, int state);
    [Obsolete("Use GetData with composite tuple")]
    StockOpnameVarianceItemDto? GetData(string varianceItemId);
}

public class StockOpnameVarianceItemDal : IStockOpnameVarianceItemDal
{
    private const string SelectColumns = """
        SessionId, BrgId, BatchNo, ExpiredDate,
        BaseSatuanId, PhysicalCountQty, SystemQtyAtCount,
        InitialVarianceQty, VerificationStatus, FinalQuantity
        """;

    private readonly DatabaseOptions _opt;

    public StockOpnameVarianceItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(StockOpnameVarianceItemDto dto)
    {
        const string sql = $"""
            INSERT INTO FARPU_StockOpnameVarianceItem (
                {SelectColumns}
            ) VALUES (
                @SessionId, @BrgId, @BatchNo, @ExpiredDate,
                @BaseSatuanId, @PhysicalCountQty, @SystemQtyAtCount,
                @InitialVarianceQty, @VerificationStatus, @FinalQuantity
            )
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Insert(IEnumerable<StockOpnameVarianceItemDto> dtos)
    {
        const string sql = $"""
            INSERT INTO FARPU_StockOpnameVarianceItem (
                {SelectColumns}
            ) VALUES (
                @SessionId, @BrgId, @BatchNo, @ExpiredDate,
                @BaseSatuanId, @PhysicalCountQty, @SystemQtyAtCount,
                @InitialVarianceQty, @VerificationStatus, @FinalQuantity
            )
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        foreach (var dto in dtos)
        {
            conn.Execute(sql, BuildParams(dto));
        }
    }

    public void Update(StockOpnameVarianceItemDto dto)
    {
        const string sql = """
            UPDATE FARPU_StockOpnameVarianceItem SET
                BaseSatuanId = @BaseSatuanId,
                PhysicalCountQty = @PhysicalCountQty,
                SystemQtyAtCount = @SystemQtyAtCount,
                InitialVarianceQty = @InitialVarianceQty,
                VerificationStatus = @VerificationStatus,
                FinalQuantity = @FinalQuantity
            WHERE SessionId = @SessionId
              AND BrgId = @BrgId
              AND BatchNo = @BatchNo
              AND ExpiredDate = @ExpiredDate
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void UpdateStatus(
        string sessionId,
        string brgId,
        string batchNo,
        string expiredDate,
        string status,
        decimal? finalQuantity = null)
    {
        const string sql = """
            UPDATE FARPU_StockOpnameVarianceItem SET
                VerificationStatus = @VerificationStatus,
                FinalQuantity = @FinalQuantity
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
        dp.AddParam("@VerificationStatus", status ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@FinalQuantity", finalQuantity, SqlDbType.Decimal);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    [Obsolete("Use UpdateStatus with composite tuple")]
    public void UpdateState(string varianceItemId, int state)
    {
        var parts = varianceItemId.Split('_');
        if (parts.Length >= 4)
        {
            UpdateStatus(parts[0], parts[1], parts[2], parts[3], state == 1 ? "Verified" : "PendingVerification");
        }
    }

    public void Delete(string sessionId)
    {
        const string sql = """
            DELETE FROM FARPU_StockOpnameVarianceItem
            WHERE SessionId = @SessionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public StockOpnameVarianceItemDto? GetData(string sessionId, string brgId, string batchNo, string expiredDate)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameVarianceItem WITH (NOLOCK)
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
        return conn.ReadSingle<StockOpnameVarianceItemDto>(sql, dp);
    }

    [Obsolete("Use GetData with composite tuple")]
    public StockOpnameVarianceItemDto? GetData(string varianceItemId)
    {
        var parts = varianceItemId.Split('_');
        if (parts.Length >= 4)
        {
            return GetData(parts[0], parts[1], parts[2], parts[3]);
        }
        return null;
    }

    public IEnumerable<StockOpnameVarianceItemDto> ListBySession(string sessionId)
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameVarianceItem WITH (NOLOCK)
            WHERE SessionId = @SessionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StockOpnameVarianceItemDto>(sql, dp);
    }

    internal static DynamicParameters BuildParams(StockOpnameVarianceItemDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", dto.SessionId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@BatchNo", dto.BatchNo, SqlDbType.VarChar);
        dp.AddParam("@ExpiredDate", dto.ExpiredDate, SqlDbType.VarChar);
        dp.AddParam("@BaseSatuanId", dto.BaseSatuanId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@PhysicalCountQty", dto.PhysicalCountQty, SqlDbType.Decimal);
        dp.AddParam("@SystemQtyAtCount", dto.SystemQtyAtCount, SqlDbType.Decimal);
        dp.AddParam("@InitialVarianceQty", dto.InitialVarianceQty, SqlDbType.Decimal);
        dp.AddParam("@VerificationStatus", dto.VerificationStatus ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@FinalQuantity", dto.FinalQuantity, SqlDbType.Decimal);
        return dp;
    }
}
