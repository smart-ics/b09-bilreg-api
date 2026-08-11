using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStokLegacyScopeDal :
    IInsert<StokLegacyScopeDto>,
    IUpdate<StokLegacyScopeDto>,
    IGetData<StokLegacyScopeDto, IStockLegacyScopeKey>
{
}

public class StokLegacyScopeDal : IStokLegacyScopeDal
{
    private const string SelectFrom = """
        SELECT
            aa.BrgId, aa.BrgMasukReffId, aa.AlignmentStatus,
            aa.TglMutasiLast, aa.LastLegacyBukuId, aa.LastSyncedAt,
            aa.InconsistencyReason,
            aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate
        FROM BILRG_StokLegacyScope aa
        """;

    private readonly DatabaseOptions _opt;

    public StokLegacyScopeDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StokLegacyScopeDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokLegacyScope(
                BrgId, BrgMasukReffId, AlignmentStatus,
                TglMutasiLast, LastLegacyBukuId, LastSyncedAt,
                InconsistencyReason,
                CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES(
                @BrgId, @BrgMasukReffId, @AlignmentStatus,
                @TglMutasiLast, @LastLegacyBukuId, @LastSyncedAt,
                @InconsistencyReason,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(StokLegacyScopeDto dto)
    {
        const string sql = """
            UPDATE BILRG_StokLegacyScope SET
                AlignmentStatus = @AlignmentStatus,
                TglMutasiLast = @TglMutasiLast,
                LastLegacyBukuId = @LastLegacyBukuId,
                LastSyncedAt = @LastSyncedAt,
                InconsistencyReason = @InconsistencyReason,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE BrgId = @BrgId
              AND BrgMasukReffId = @BrgMasukReffId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public StokLegacyScopeDto GetData(IStockLegacyScopeKey key)
    {
        var sql = $"{SelectFrom}\nWHERE aa.BrgId = @BrgId AND aa.BrgMasukReffId = @BrgMasukReffId";
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", key.BrgId, SqlDbType.VarChar);
        dp.AddParam("@BrgMasukReffId", key.BrgMasukReffId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StokLegacyScopeDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(StokLegacyScopeDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@BrgMasukReffId", dto.BrgMasukReffId, SqlDbType.VarChar);
        dp.AddParam("@AlignmentStatus", dto.AlignmentStatus, SqlDbType.Int);
        dp.AddParam("@TglMutasiLast", dto.TglMutasiLast, SqlDbType.DateTime);
        dp.AddParam("@LastLegacyBukuId", dto.LastLegacyBukuId, SqlDbType.VarChar);
        dp.AddParam("@LastSyncedAt", dto.LastSyncedAt, SqlDbType.DateTime);
        dp.AddParam("@InconsistencyReason", dto.InconsistencyReason, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        return dp;
    }
}
