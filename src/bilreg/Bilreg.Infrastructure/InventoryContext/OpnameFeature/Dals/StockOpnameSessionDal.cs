using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.OpnameFeature;
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
    IEnumerable<StockOpnameSessionDto> ListData(string? layananId = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null);
    IEnumerable<StockOpnameSessionDto> ListData(string? layananId, int? state, DateTime? fromDate = null, DateTime? toDate = null);
    void Delete(string sessionId);
}

public class StockOpnameSessionDal : IStockOpnameSessionDal
{
    private const string SelectColumns = """
        SessionId, LayananId, ScopeType, Status, CutoffTime,
        StartedAt, CountingFinishedAt,
        VerifiedAt,
        ApprovedAt, ApprovedBy,
        RejectedAt, RejectedBy, RejectionNotes, RejectionDisposition,
        CancelledAt, CancelledBy, CancellationReason,
        CreatedBy, CreatedAt
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
                @SessionId, @LayananId, @ScopeType, @Status, @CutoffTime,
                @StartedAt, @CountingFinishedAt,
                @VerifiedAt,
                @ApprovedAt, @ApprovedBy,
                @RejectedAt, @RejectedBy, @RejectionNotes, @RejectionDisposition,
                @CancelledAt, @CancelledBy, @CancellationReason,
                @CreatedBy, @CreatedAt
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
                ScopeType = @ScopeType,
                Status = @Status,
                CutoffTime = @CutoffTime,
                StartedAt = @StartedAt,
                CountingFinishedAt = @CountingFinishedAt,
                VerifiedAt = @VerifiedAt,
                ApprovedAt = @ApprovedAt,
                ApprovedBy = @ApprovedBy,
                RejectedAt = @RejectedAt,
                RejectedBy = @RejectedBy,
                RejectionNotes = @RejectionNotes,
                RejectionDisposition = @RejectionDisposition,
                CancelledAt = @CancelledAt,
                CancelledBy = @CancelledBy,
                CancellationReason = @CancellationReason,
                CreatedBy = @CreatedBy,
                CreatedAt = @CreatedAt
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
              AND Status NOT IN ('Approved', 'Rejected', 'Cancelled')
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@LayananId", layananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql, dp) > 0;
    }

    public IEnumerable<StockOpnameSessionDto> ListData(
        string? layananId = null,
        string? status = null,
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

        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += " AND Status = @Status";
            dp.AddParam("@Status", status, SqlDbType.VarChar);
        }

        if (fromDate.HasValue)
        {
            sql += " AND CutoffTime >= @FromDate";
            dp.AddParam("@FromDate", fromDate.Value, SqlDbType.DateTime);
        }

        if (toDate.HasValue)
        {
            sql += " AND CutoffTime <= @ToDate";
            dp.AddParam("@ToDate", toDate.Value, SqlDbType.DateTime);
        }

        sql += " ORDER BY CreatedAt DESC";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StockOpnameSessionDto>(sql, dp);
    }

    public IEnumerable<StockOpnameSessionDto> ListData(
        string? layananId,
        int? state,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var statusStr = state.HasValue ? ((SessionStatusEnum)state.Value).ToString() : null;
        return ListData(layananId, statusStr, fromDate, toDate);
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

    internal static DynamicParameters BuildParams(StockOpnameSessionDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", dto.SessionId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@ScopeType", dto.ScopeType, SqlDbType.VarChar);
        dp.AddParam("@Status", dto.Status, SqlDbType.VarChar);
        var cutoffTime = dto.CutoffTime > new DateTime(1753, 1, 1) ? dto.CutoffTime : DateTime.Now;
        dp.AddParam("@CutoffTime", cutoffTime, SqlDbType.DateTime);
        dp.AddParam("@StartedAt", dto.StartedAt, SqlDbType.DateTime);
        dp.AddParam("@CountingFinishedAt", dto.CountingFinishedAt, SqlDbType.DateTime);
        dp.AddParam("@VerifiedAt", dto.VerifiedAt, SqlDbType.DateTime);
        dp.AddParam("@ApprovedAt", dto.ApprovedAt, SqlDbType.DateTime);
        dp.AddParam("@ApprovedBy", dto.ApprovedBy ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@RejectedAt", dto.RejectedAt, SqlDbType.DateTime);
        dp.AddParam("@RejectedBy", dto.RejectedBy ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@RejectionNotes", dto.RejectionNotes ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@RejectionDisposition", dto.RejectionDisposition ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@CancelledAt", dto.CancelledAt, SqlDbType.DateTime);
        dp.AddParam("@CancelledBy", dto.CancelledBy ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@CancellationReason", dto.CancellationReason ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@CreatedBy", dto.CreatedBy, SqlDbType.VarChar);
        dp.AddParam("@CreatedAt", dto.CreatedAt, SqlDbType.DateTime);
        return dp;
    }
}
