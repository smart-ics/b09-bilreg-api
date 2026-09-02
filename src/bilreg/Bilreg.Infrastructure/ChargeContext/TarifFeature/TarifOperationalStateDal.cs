using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface ITarifOperationalStateDal
{
    TarifOperationalStateDto GetState();

    void UpsertState(TarifOperationalStateDto dto);
}

public class TarifOperationalStateDal : ITarifOperationalStateDal
{
    private const int SingleRowId = 1;
    private readonly DatabaseOptions _opt;

    public TarifOperationalStateDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public TarifOperationalStateDto GetState()
    {
        const string sql = """
            SELECT
                RowId, MigrationMode, LastImportAt, LastImportBy,
                LastBaselineAt, LastBaselinePolicyId, UpdatedAt, UpdatedBy
            FROM BILRG_TarifOperationalState
            WHERE RowId = @RowId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RowId", SingleRowId, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var row = conn.Read<TarifOperationalStateDto>(sql, dp)?.FirstOrDefault();
        return row ?? new TarifOperationalStateDto(
            SingleRowId, null, null, "", null, "", DateTime.Now, "SYSTEM");
    }

    public void UpsertState(TarifOperationalStateDto dto)
    {
        const string sql = """
            IF EXISTS (SELECT 1 FROM BILRG_TarifOperationalState WHERE RowId = @RowId)
                UPDATE BILRG_TarifOperationalState SET
                    MigrationMode = @MigrationMode,
                    LastImportAt = @LastImportAt,
                    LastImportBy = @LastImportBy,
                    LastBaselineAt = @LastBaselineAt,
                    LastBaselinePolicyId = @LastBaselinePolicyId,
                    UpdatedAt = @UpdatedAt,
                    UpdatedBy = @UpdatedBy
                WHERE RowId = @RowId
            ELSE
                INSERT INTO BILRG_TarifOperationalState (
                    RowId, MigrationMode, LastImportAt, LastImportBy,
                    LastBaselineAt, LastBaselinePolicyId, UpdatedAt, UpdatedBy)
                VALUES (
                    @RowId, @MigrationMode, @LastImportAt, @LastImportBy,
                    @LastBaselineAt, @LastBaselinePolicyId, @UpdatedAt, @UpdatedBy)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    private static DynamicParameters MapParams(TarifOperationalStateDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@RowId", dto.RowId, SqlDbType.Int);
        dp.AddParam("@MigrationMode", dto.MigrationMode, SqlDbType.Int);
        dp.AddParam("@LastImportAt", dto.LastImportAt, SqlDbType.DateTime);
        dp.AddParam("@LastImportBy", dto.LastImportBy ?? "", SqlDbType.VarChar);
        dp.AddParam("@LastBaselineAt", dto.LastBaselineAt, SqlDbType.DateTime);
        dp.AddParam("@LastBaselinePolicyId", dto.LastBaselinePolicyId ?? "", SqlDbType.VarChar);
        dp.AddParam("@UpdatedAt", dto.UpdatedAt, SqlDbType.DateTime);
        dp.AddParam("@UpdatedBy", dto.UpdatedBy ?? "", SqlDbType.VarChar);
        return dp;
    }
}
