using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.Shared;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;

public interface IBedOperationalDal :
    IInsert<BedOperationalDto>,
    IUpdate<BedOperationalDto>,
    IGetData<BedOperationalDto, IBedOperationalKey>
{
    int UpdateConditional(BedOperationalDto dto, int expectedVersion);
}

public class BedOperationalDal : IBedOperationalDal
{
    private const string SelectFrom = """
        SELECT
            aa.BedId, aa.BangsalId, aa.KamarId,
            aa.OccupancyPolicyId, aa.OccupancyPolicyName,
            aa.CurrentReadiness, aa.LatestReadinessTransactionId,
            aa.IsBlocked, aa.CurrentRestrictionType,
            aa.CurrentBlockerReason, aa.CurrentBlockerTransactionId,
            aa.OccupancyEpoch, aa.Version,
            aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate,
            aa.VodUser, aa.VodDate
        FROM BILRG_RnaBedOperational aa
        """;

    private readonly DatabaseOptions _opt;
    private readonly ISqlConnectionScope _connectionScope;

    public BedOperationalDal(IOptions<DatabaseOptions> opt, ISqlConnectionScope? connectionScope = null)
    {
        _opt = opt.Value;
        _connectionScope = connectionScope ?? new SqlConnectionScope(opt);
    }

    private SqlConnection GetConnection() => _connectionScope.GetConnection();

    public void Insert(BedOperationalDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_RnaBedOperational(
                BedId, BangsalId, KamarId,
                OccupancyPolicyId, OccupancyPolicyName,
                CurrentReadiness, LatestReadinessTransactionId,
                IsBlocked, CurrentRestrictionType,
                CurrentBlockerReason, CurrentBlockerTransactionId,
                OccupancyEpoch, Version,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES(
                @BedId, @BangsalId, @KamarId,
                @OccupancyPolicyId, @OccupancyPolicyName,
                @CurrentReadiness, @LatestReadinessTransactionId,
                @IsBlocked, @CurrentRestrictionType,
                @CurrentBlockerReason, @CurrentBlockerTransactionId,
                @OccupancyEpoch, @Version,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;
        var conn = GetConnection();
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(BedOperationalDto dto)
    {
        const string sql = """
            UPDATE BILRG_RnaBedOperational SET
                BangsalId = @BangsalId,
                KamarId = @KamarId,
                OccupancyPolicyId = @OccupancyPolicyId,
                OccupancyPolicyName = @OccupancyPolicyName,
                CurrentReadiness = @CurrentReadiness,
                LatestReadinessTransactionId = @LatestReadinessTransactionId,
                IsBlocked = @IsBlocked,
                CurrentRestrictionType = @CurrentRestrictionType,
                CurrentBlockerReason = @CurrentBlockerReason,
                CurrentBlockerTransactionId = @CurrentBlockerTransactionId,
                OccupancyEpoch = @OccupancyEpoch,
                Version = @Version,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE BedId = @BedId
            """;
        var conn = GetConnection();
        conn.Execute(sql, BuildParams(dto));
    }

    public int UpdateConditional(BedOperationalDto dto, int expectedVersion)
    {
        const string sql = """
            UPDATE BILRG_RnaBedOperational SET
                BangsalId = @BangsalId,
                KamarId = @KamarId,
                OccupancyPolicyId = @OccupancyPolicyId,
                OccupancyPolicyName = @OccupancyPolicyName,
                CurrentReadiness = @CurrentReadiness,
                LatestReadinessTransactionId = @LatestReadinessTransactionId,
                IsBlocked = @IsBlocked,
                CurrentRestrictionType = @CurrentRestrictionType,
                CurrentBlockerReason = @CurrentBlockerReason,
                CurrentBlockerTransactionId = @CurrentBlockerTransactionId,
                OccupancyEpoch = @OccupancyEpoch,
                Version = @Version,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE BedId = @BedId
                AND Version = @ExpectedVersion
            """;
        var dp = BuildParams(dto);
        dp.AddParam("@ExpectedVersion", expectedVersion, SqlDbType.Int);
        var conn = GetConnection();
        return conn.Execute(sql, dp);
    }

    public BedOperationalDto GetData(IBedOperationalKey key)
    {
        var sql = $"{SelectFrom}\nWHERE aa.BedId = @BedId";
        var dp = new DynamicParameters();
        dp.AddParam("@BedId", key.BedId, SqlDbType.VarChar);
        var conn = GetConnection();
        return conn.ReadSingle<BedOperationalDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(BedOperationalDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@BedId", dto.BedId, SqlDbType.VarChar);
        dp.AddParam("@BangsalId", dto.BangsalId, SqlDbType.VarChar);
        dp.AddParam("@KamarId", dto.KamarId, SqlDbType.VarChar);
        dp.AddParam("@OccupancyPolicyId", dto.OccupancyPolicyId, SqlDbType.VarChar);
        dp.AddParam("@OccupancyPolicyName", dto.OccupancyPolicyName, SqlDbType.VarChar);
        dp.AddParam("@CurrentReadiness", dto.CurrentReadiness, SqlDbType.Int);
        dp.AddParam("@LatestReadinessTransactionId", dto.LatestReadinessTransactionId, SqlDbType.VarChar);
        dp.AddParam("@IsBlocked", dto.IsBlocked, SqlDbType.Bit);
        dp.AddParam("@CurrentRestrictionType", dto.CurrentRestrictionType, SqlDbType.Int);
        dp.AddParam("@CurrentBlockerReason", dto.CurrentBlockerReason, SqlDbType.VarChar);
        dp.AddParam("@CurrentBlockerTransactionId", dto.CurrentBlockerTransactionId, SqlDbType.VarChar);
        dp.AddParam("@OccupancyEpoch", dto.OccupancyEpoch, SqlDbType.BigInt);
        dp.AddParam("@Version", dto.Version, SqlDbType.Int);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
