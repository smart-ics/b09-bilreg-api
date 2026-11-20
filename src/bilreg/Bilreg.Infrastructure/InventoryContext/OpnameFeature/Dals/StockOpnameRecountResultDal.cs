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
    void DeleteByVarianceItem(string varianceItemId);
    StockOpnameRecountResultDto? GetData(string recountResultId);
    StockOpnameRecountResultDto? GetByVarianceItem(string varianceItemId);
    IEnumerable<StockOpnameRecountResultDto> ListByVarianceItems(IEnumerable<string> varianceItemIds);
}

public class StockOpnameRecountResultDal : IStockOpnameRecountResultDal
{
    private const string SelectColumns = """
        RecountResultId, VarianceItemId, RecountQty, ReconstructedSystemQty,
        FinalVarianceQty, FinalValuation, DiscrepancyReason,
        Notes, RecountedBy, RecountTime
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
                @RecountResultId, @VarianceItemId, @RecountQty, @ReconstructedSystemQty,
                @FinalVarianceQty, @FinalValuation, @DiscrepancyReason,
                @Notes, @RecountedBy, @RecountTime
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
                ReconstructedSystemQty = @ReconstructedSystemQty,
                FinalVarianceQty = @FinalVarianceQty,
                FinalValuation = @FinalValuation,
                DiscrepancyReason = @DiscrepancyReason,
                Notes = @Notes,
                RecountedBy = @RecountedBy,
                RecountTime = @RecountTime
            WHERE VarianceItemId = @VarianceItemId
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

    public void DeleteByVarianceItem(string varianceItemId)
    {
        const string sql = """
            DELETE FROM FARPU_StockOpnameRecountResult
            WHERE VarianceItemId = @VarianceItemId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@VarianceItemId", varianceItemId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
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

    public StockOpnameRecountResultDto? GetByVarianceItem(string varianceItemId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameRecountResult WITH (NOLOCK)
            WHERE VarianceItemId = @VarianceItemId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@VarianceItemId", varianceItemId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockOpnameRecountResultDto>(sql, dp);
    }

    public IEnumerable<StockOpnameRecountResultDto> ListByVarianceItems(IEnumerable<string> varianceItemIds)
    {
        var idList = varianceItemIds?.ToList() ?? [];
        if (idList.Count == 0)
            return [];

        const string sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameRecountResult WITH (NOLOCK)
            WHERE VarianceItemId IN @VarianceItemIds
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<StockOpnameRecountResultDto>(sql, new { VarianceItemIds = idList });
    }

    private static DynamicParameters BuildParams(StockOpnameRecountResultDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@RecountResultId", dto.RecountResultId, SqlDbType.VarChar);
        dp.AddParam("@VarianceItemId", dto.VarianceItemId, SqlDbType.VarChar);
        dp.AddParam("@RecountQty", dto.RecountQty, SqlDbType.Decimal);
        dp.AddParam("@ReconstructedSystemQty", dto.ReconstructedSystemQty, SqlDbType.Decimal);
        dp.AddParam("@FinalVarianceQty", dto.FinalVarianceQty, SqlDbType.Decimal);
        dp.AddParam("@FinalValuation", dto.FinalValuation, SqlDbType.Decimal);
        dp.AddParam("@DiscrepancyReason", dto.DiscrepancyReason, SqlDbType.Int);
        dp.AddParam("@Notes", dto.Notes, SqlDbType.VarChar);
        dp.AddParam("@RecountedBy", dto.RecountedBy, SqlDbType.VarChar);
        dp.AddParam("@RecountTime", dto.RecountTime, SqlDbType.DateTime);
        return dp;
    }
}
