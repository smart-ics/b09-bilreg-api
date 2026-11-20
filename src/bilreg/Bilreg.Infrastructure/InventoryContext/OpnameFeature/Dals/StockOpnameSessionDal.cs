using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dals;

public interface IStockOpnameSessionDal :
    IInsert<StockOpnameSessionDto>,
    IUpdate<StockOpnameSessionDto>
{
    StockOpnameSessionDto? GetData(string sessionId);
    bool HasActiveSessionAtLocation(string layananId);
    IEnumerable<StockOpnameSessionDto> ListData(string? layananId = null, int? state = null, DateTime? fromDate = null, DateTime? toDate = null);
    void Delete(string sessionId);
}

public class StockOpnameSessionDal : IStockOpnameSessionDal
{
    private const string SelectColumns = """
        SessionId, LayananId, ScopeMode, State, CutoffDate,
        CreatedBy, CreatedAt,
        CountingSubmittedBy, CountingSubmittedAt,
        VerifiedBy, VerifiedAt,
        ApprovedBy, ApprovedAt,
        RejectedBy, RejectedAt,
        CancelledBy, CancelledAt,
        CancelReason, RevisionNotes
        """;

    private readonly DatabaseOptions _opt;

    public StockOpnameSessionDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(StockOpnameSessionDto dto)
    {
        const string sql = $"""
            INSERT INTO FARPU_StockOpnameSession (
                {SelectColumns}
            ) VALUES (
                @SessionId, @LayananId, @ScopeMode, @State, @CutoffDate,
                @CreatedBy, @CreatedAt,
                @CountingSubmittedBy, @CountingSubmittedAt,
                @VerifiedBy, @VerifiedAt,
                @ApprovedBy, @ApprovedAt,
                @RejectedBy, @RejectedAt,
                @CancelledBy, @CancelledAt,
                @CancelReason, @RevisionNotes
            )
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(StockOpnameSessionDto dto)
    {
        const string sql = """
            UPDATE FARPU_StockOpnameSession SET
                LayananId = @LayananId,
                ScopeMode = @ScopeMode,
                State = @State,
                CutoffDate = @CutoffDate,
                CreatedBy = @CreatedBy,
                CreatedAt = @CreatedAt,
                CountingSubmittedBy = @CountingSubmittedBy,
                CountingSubmittedAt = @CountingSubmittedAt,
                VerifiedBy = @VerifiedBy,
                VerifiedAt = @VerifiedAt,
                ApprovedBy = @ApprovedBy,
                ApprovedAt = @ApprovedAt,
                RejectedBy = @RejectedBy,
                RejectedAt = @RejectedAt,
                CancelledBy = @CancelledBy,
                CancelledAt = @CancelledAt,
                CancelReason = @CancelReason,
                RevisionNotes = @RevisionNotes
            WHERE SessionId = @SessionId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public StockOpnameSessionDto? GetData(string sessionId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameSession WITH (NOLOCK)
            WHERE SessionId = @SessionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockOpnameSessionDto>(sql, dp);
    }

    public bool HasActiveSessionAtLocation(string layananId)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM FARPU_StockOpnameSession WITH (NOLOCK)
            WHERE LayananId = @LayananId
              AND State IN (0, 1, 2, 3, 4)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@LayananId", layananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql, dp) > 0;
    }

    public IEnumerable<StockOpnameSessionDto> ListData(
        string? layananId = null,
        int? state = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameSession WITH (NOLOCK)
            WHERE 1 = 1
            """;

        var dp = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(layananId))
        {
            sql += " AND LayananId = @LayananId";
            dp.AddParam("@LayananId", layananId, SqlDbType.VarChar);
        }

        if (state.HasValue)
        {
            sql += " AND State = @State";
            dp.AddParam("@State", state.Value, SqlDbType.Int);
        }

        if (fromDate.HasValue)
        {
            sql += " AND CutoffDate >= @FromDate";
            dp.AddParam("@FromDate", fromDate.Value, SqlDbType.DateTime);
        }

        if (toDate.HasValue)
        {
            sql += " AND CutoffDate <= @ToDate";
            dp.AddParam("@ToDate", toDate.Value, SqlDbType.DateTime);
        }

        sql += " ORDER BY CreatedAt DESC";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StockOpnameSessionDto>(sql, dp);
    }

    public void Delete(string sessionId)
    {
        const string sql = """
            DELETE FROM FARPU_StockOpnameSession
            WHERE SessionId = @SessionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    private static DynamicParameters BuildParams(StockOpnameSessionDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", dto.SessionId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@ScopeMode", dto.ScopeMode, SqlDbType.Int);
        dp.AddParam("@State", dto.State, SqlDbType.Int);
        dp.AddParam("@CutoffDate", dto.CutoffDate, SqlDbType.DateTime);
        dp.AddParam("@CreatedBy", dto.CreatedBy, SqlDbType.VarChar);
        dp.AddParam("@CreatedAt", dto.CreatedAt, SqlDbType.DateTime);
        dp.AddParam("@CountingSubmittedBy", dto.CountingSubmittedBy, SqlDbType.VarChar);
        dp.AddParam("@CountingSubmittedAt", dto.CountingSubmittedAt, SqlDbType.DateTime);
        dp.AddParam("@VerifiedBy", dto.VerifiedBy, SqlDbType.VarChar);
        dp.AddParam("@VerifiedAt", dto.VerifiedAt, SqlDbType.DateTime);
        dp.AddParam("@ApprovedBy", dto.ApprovedBy, SqlDbType.VarChar);
        dp.AddParam("@ApprovedAt", dto.ApprovedAt, SqlDbType.DateTime);
        dp.AddParam("@RejectedBy", dto.RejectedBy, SqlDbType.VarChar);
        dp.AddParam("@RejectedAt", dto.RejectedAt, SqlDbType.DateTime);
        dp.AddParam("@CancelledBy", dto.CancelledBy, SqlDbType.VarChar);
        dp.AddParam("@CancelledAt", dto.CancelledAt, SqlDbType.DateTime);
        dp.AddParam("@CancelReason", dto.CancelReason, SqlDbType.VarChar);
        dp.AddParam("@RevisionNotes", dto.RevisionNotes, SqlDbType.VarChar);
        return dp;
    }
}
