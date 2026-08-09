using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStockLayerLegacyBindingDal :
    IInsert<StockLayerLegacyBindingDto>,
    IUpdate<StockLayerLegacyBindingDto>,
    IGetData<StockLayerLegacyBindingDto, string>
{
    IReadOnlyList<StockLayerLegacyBindingDto> ListByLedgerScope(IStockLedgerScopeKey scope);

    IReadOnlyList<StockLayerLegacyBindingDto> ListByWriteScope(IStockWriteScopeKey writeScope);
}

public class StockLayerLegacyBindingDal : IStockLayerLegacyBindingDal
{
    private readonly DatabaseOptions _opt;

    public StockLayerLegacyBindingDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StockLayerLegacyBindingDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokLayerLegacyBinding (
                StockLayerId, LegacyRowId, BrgId, ReceiptSourceId, LayananId,
                AmountPerUnit, ExpirationDate, Batch, BoundAt,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @StockLayerId, @LegacyRowId, @BrgId, @ReceiptSourceId, @LayananId,
                @AmountPerUnit, @ExpirationDate, @Batch, @BoundAt,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public void Update(StockLayerLegacyBindingDto dto)
    {
        const string sql = """
            UPDATE BILRG_StokLayerLegacyBinding SET
                LegacyRowId = @LegacyRowId,
                BrgId = @BrgId,
                ReceiptSourceId = @ReceiptSourceId,
                LayananId = @LayananId,
                AmountPerUnit = @AmountPerUnit,
                ExpirationDate = @ExpirationDate,
                Batch = @Batch,
                BoundAt = @BoundAt,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE StockLayerId = @StockLayerId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public StockLayerLegacyBindingDto GetData(string stockLayerId)
    {
        const string sql = """
            SELECT
                aa.StockLayerId, aa.LegacyRowId, aa.BrgId, aa.ReceiptSourceId, aa.LayananId,
                aa.AmountPerUnit, aa.ExpirationDate, aa.Batch, aa.BoundAt,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokLayerLegacyBinding aa
            WHERE aa.StockLayerId = @StockLayerId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@StockLayerId", stockLayerId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockLayerLegacyBindingDto>(sql, dp);
    }

    public IReadOnlyList<StockLayerLegacyBindingDto> ListByLedgerScope(IStockLedgerScopeKey scope)
    {
        const string sql = """
            SELECT
                aa.StockLayerId, aa.LegacyRowId, aa.BrgId, aa.ReceiptSourceId, aa.LayananId,
                aa.AmountPerUnit, aa.ExpirationDate, aa.Batch, aa.BoundAt,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokLayerLegacyBinding aa
            WHERE
                aa.BrgId = @BrgId
                AND aa.ReceiptSourceId = @ReceiptSourceId
            ORDER BY aa.LayananId ASC, aa.StockLayerId ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", scope.BrgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", scope.ReceiptSourceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return (conn.Read<StockLayerLegacyBindingDto>(sql, dp) ?? []).ToList();
    }

    public IReadOnlyList<StockLayerLegacyBindingDto> ListByWriteScope(IStockWriteScopeKey writeScope)
    {
        const string sql = """
            SELECT
                aa.StockLayerId, aa.LegacyRowId, aa.BrgId, aa.ReceiptSourceId, aa.LayananId,
                aa.AmountPerUnit, aa.ExpirationDate, aa.Batch, aa.BoundAt,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokLayerLegacyBinding aa
            WHERE
                aa.BrgId = @BrgId
                AND aa.ReceiptSourceId = @ReceiptSourceId
                AND aa.LayananId = @LayananId
            ORDER BY aa.StockLayerId ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", writeScope.BrgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", writeScope.ReceiptSourceId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", writeScope.LayananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return (conn.Read<StockLayerLegacyBindingDto>(sql, dp) ?? []).ToList();
    }

    private static DynamicParameters MapParams(StockLayerLegacyBindingDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@StockLayerId", dto.StockLayerId, SqlDbType.VarChar);
        dp.AddParam("@LegacyRowId", dto.LegacyRowId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", dto.ReceiptSourceId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@AmountPerUnit", dto.AmountPerUnit, SqlDbType.Decimal);
        dp.AddParam("@ExpirationDate", dto.ExpirationDate, SqlDbType.DateTime);
        dp.AddParam("@Batch", dto.Batch, SqlDbType.VarChar);
        dp.AddParam("@BoundAt", dto.BoundAt, SqlDbType.DateTime);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
