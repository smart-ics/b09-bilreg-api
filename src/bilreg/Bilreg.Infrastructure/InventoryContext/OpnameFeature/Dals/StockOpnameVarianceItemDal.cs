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
    void UpdateState(string varianceItemId, int state);
    void Delete(string sessionId);
    StockOpnameVarianceItemDto? GetData(string varianceItemId);
    IEnumerable<StockOpnameVarianceItemDto> ListBySession(string sessionId);
}

public class StockOpnameVarianceItemDal : IStockOpnameVarianceItemDal
{
    private const string SelectColumns = """
        VarianceItemId, SessionId, BrgId, BatchNo, ExpiredDate,
        BaseSatuanId, CountQty, SystemQty, VarianceQty,
        Hpp, VarianceValuation, State
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
                @VarianceItemId, @SessionId, @BrgId, @BatchNo, @ExpiredDate,
                @BaseSatuanId, @CountQty, @SystemQty, @VarianceQty,
                @Hpp, @VarianceValuation, @State
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
                @VarianceItemId, @SessionId, @BrgId, @BatchNo, @ExpiredDate,
                @BaseSatuanId, @CountQty, @SystemQty, @VarianceQty,
                @Hpp, @VarianceValuation, @State
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
                CountQty = @CountQty,
                SystemQty = @SystemQty,
                VarianceQty = @VarianceQty,
                Hpp = @Hpp,
                VarianceValuation = @VarianceValuation,
                State = @State
            WHERE VarianceItemId = @VarianceItemId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void UpdateState(string varianceItemId, int state)
    {
        const string sql = """
            UPDATE FARPU_StockOpnameVarianceItem SET
                State = @State
            WHERE VarianceItemId = @VarianceItemId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@VarianceItemId", varianceItemId, SqlDbType.VarChar);
        dp.AddParam("@State", state, SqlDbType.Int);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
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

    public StockOpnameVarianceItemDto? GetData(string varianceItemId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM FARPU_StockOpnameVarianceItem WITH (NOLOCK)
            WHERE VarianceItemId = @VarianceItemId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@VarianceItemId", varianceItemId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockOpnameVarianceItemDto>(sql, dp);
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

    private static DynamicParameters BuildParams(StockOpnameVarianceItemDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@VarianceItemId", dto.VarianceItemId, SqlDbType.VarChar);
        dp.AddParam("@SessionId", dto.SessionId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@BatchNo", dto.BatchNo, SqlDbType.VarChar);
        dp.AddParam("@ExpiredDate", dto.ExpiredDate, SqlDbType.VarChar);
        dp.AddParam("@BaseSatuanId", dto.BaseSatuanId, SqlDbType.VarChar);
        dp.AddParam("@CountQty", dto.CountQty, SqlDbType.Decimal);
        dp.AddParam("@SystemQty", dto.SystemQty, SqlDbType.Decimal);
        dp.AddParam("@VarianceQty", dto.VarianceQty, SqlDbType.Decimal);
        dp.AddParam("@Hpp", dto.Hpp, SqlDbType.Decimal);
        dp.AddParam("@VarianceValuation", dto.VarianceValuation, SqlDbType.Decimal);
        dp.AddParam("@State", dto.State, SqlDbType.Int);
        return dp;
    }
}
