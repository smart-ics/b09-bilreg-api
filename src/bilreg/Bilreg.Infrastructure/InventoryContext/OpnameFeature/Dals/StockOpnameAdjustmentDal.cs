using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dals;

public interface IStockOpnameAdjustmentDal :
    IInsert<StockOpnameAdjustmentDto>
{
    void Insert(IEnumerable<StockOpnameAdjustmentDto> dtos);
    StockOpnameAdjustmentDto? GetData(string adjustmentId);
    IEnumerable<StockOpnameAdjustmentDto> ListBySession(string sessionId);
    bool ExistsForSession(string sessionId);
    bool ExistsForTuple(string sessionId, string brgId, string batchNo, string expiredDate);
}

public class StockOpnameAdjustmentDal : IStockOpnameAdjustmentDal
{
    private const string SelectColumns = """
        AdjustmentId, SessionId, BrgId, BatchNo, ExpiredDate, LayananId,
        AdjustmentQty, BaseSatuanId, Hpp, NilaiFinansial,
        StokMutasiId, LegacyBukuId, MovementKind, ExecutedAt, ExecutedBy
        """;

    private readonly DatabaseOptions _opt;

    public StockOpnameAdjustmentDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(StockOpnameAdjustmentDto dto)
    {
        const string sql = $"""
            INSERT INTO dbo.BILRG_StockOpnameAdjustment (
                {SelectColumns}
            ) VALUES (
                @AdjustmentId, @SessionId, @BrgId, @BatchNo, @ExpiredDate, @LayananId,
                @AdjustmentQty, @BaseSatuanId, @Hpp, @NilaiFinansial,
                @StokMutasiId, @LegacyBukuId, @MovementKind, @ExecutedAt, @ExecutedBy
            )
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Insert(IEnumerable<StockOpnameAdjustmentDto> dtos)
    {
        const string sql = $"""
            INSERT INTO dbo.BILRG_StockOpnameAdjustment (
                {SelectColumns}
            ) VALUES (
                @AdjustmentId, @SessionId, @BrgId, @BatchNo, @ExpiredDate, @LayananId,
                @AdjustmentQty, @BaseSatuanId, @Hpp, @NilaiFinansial,
                @StokMutasiId, @LegacyBukuId, @MovementKind, @ExecutedAt, @ExecutedBy
            )
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        foreach (var dto in dtos)
        {
            conn.Execute(sql, BuildParams(dto));
        }
    }

    public StockOpnameAdjustmentDto? GetData(string adjustmentId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.BILRG_StockOpnameAdjustment WITH (NOLOCK)
            WHERE AdjustmentId = @AdjustmentId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@AdjustmentId", adjustmentId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockOpnameAdjustmentDto>(sql, dp);
    }

    public IEnumerable<StockOpnameAdjustmentDto> ListBySession(string sessionId)
    {
        const string sql = $"""
            SELECT {SelectColumns}
            FROM dbo.BILRG_StockOpnameAdjustment WITH (NOLOCK)
            WHERE SessionId = @SessionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StockOpnameAdjustmentDto>(sql, dp);
    }

    public bool ExistsForSession(string sessionId)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.BILRG_StockOpnameAdjustment WITH (NOLOCK)
            WHERE SessionId = @SessionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SessionId", sessionId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql, dp) > 0;
    }

    public bool ExistsForTuple(string sessionId, string brgId, string batchNo, string expiredDate)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.BILRG_StockOpnameAdjustment WITH (NOLOCK)
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
        return conn.ExecuteScalar<int>(sql, dp) > 0;
    }

    private static DynamicParameters BuildParams(StockOpnameAdjustmentDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@AdjustmentId", dto.AdjustmentId, SqlDbType.VarChar);
        dp.AddParam("@SessionId", dto.SessionId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@BatchNo", dto.BatchNo, SqlDbType.VarChar);
        dp.AddParam("@ExpiredDate", dto.ExpiredDate, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@AdjustmentQty", dto.AdjustmentQty, SqlDbType.Decimal);
        dp.AddParam("@BaseSatuanId", dto.BaseSatuanId, SqlDbType.VarChar);
        dp.AddParam("@Hpp", dto.Hpp, SqlDbType.Decimal);
        dp.AddParam("@NilaiFinansial", dto.NilaiFinansial, SqlDbType.Decimal);
        dp.AddParam("@StokMutasiId", dto.StokMutasiId, SqlDbType.VarChar);
        dp.AddParam("@LegacyBukuId", dto.LegacyBukuId, SqlDbType.VarChar);
        dp.AddParam("@MovementKind", dto.MovementKind, SqlDbType.Int);
        dp.AddParam("@ExecutedAt", dto.ExecutedAt, SqlDbType.DateTime);
        dp.AddParam("@ExecutedBy", dto.ExecutedBy, SqlDbType.VarChar);
        return dp;
    }
}
