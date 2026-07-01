using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.IgdContext.BedIgdFeature;

public interface IBedIgdDal :
    IInsert<BedIgdDto>,
    IUpdate<BedIgdDto>,
    IDelete<IBedIgdKey>,
    IGetData<BedIgdDto, IBedIgdKey>,
    IListData<BedIgdDto>
{
    int UpdateOccupancyConditional(BedIgdDto dto, string priorState, string priorVisitId);
    IEnumerable<BedIgdDto> ListAvailable();
}

public class BedIgdDal : IBedIgdDal
{
    private readonly DatabaseOptions _opt;

    public BedIgdDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BedIgdDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_BedIgd (
                BedIgdId, BedIgdName, KamarName,
                BedState, CurrentIgdVisitId,
                OccupyUser, OccupyDateTime,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @BedIgdId, @BedIgdName, @KamarName,
                @BedState, @CurrentIgdVisitId,
                @OccupyUser, @OccupyDateTime,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BedIgdDto dto)
    {
        const string sql = """
            UPDATE BILRG_BedIgd
            SET BedIgdName = @BedIgdName,
                KamarName = @KamarName,
                BedState = @BedState,
                CurrentIgdVisitId = @CurrentIgdVisitId,
                OccupyUser = @OccupyUser,
                OccupyDateTime = @OccupyDateTime,
                CrtUser = @CrtUser, CrtDate = @CrtDate,
                UpdUser = @UpdUser, UpdDate = @UpdDate,
                VodUser = @VodUser, VodDate = @VodDate
            WHERE BedIgdId = @BedIgdId
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public int UpdateOccupancyConditional(BedIgdDto dto, string priorState, string priorVisitId)
    {
        const string sql = """
            UPDATE BILRG_BedIgd
            SET BedState = @BedState,
                CurrentIgdVisitId = @CurrentIgdVisitId,
                OccupyUser = @OccupyUser,
                OccupyDateTime = @OccupyDateTime,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE BedIgdId = @BedIgdId
              AND BedState = @PriorState
              AND CurrentIgdVisitId = @PriorVisitId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BedIgdId", dto.BedIgdId, SqlDbType.VarChar);
        dp.AddParam("@BedState", dto.BedState, SqlDbType.VarChar);
        dp.AddParam("@CurrentIgdVisitId", dto.CurrentIgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@OccupyUser", dto.OccupyUser, SqlDbType.VarChar);
        dp.AddParam("@OccupyDateTime", dto.OccupyDateTime, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@PriorState", priorState, SqlDbType.VarChar);
        dp.AddParam("@PriorVisitId", priorVisitId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public void Delete(IBedIgdKey key)
    {
        const string sql = "DELETE BILRG_BedIgd WHERE BedIgdId = @BedIgdId";
        var dp = new DynamicParameters();
        dp.AddParam("@BedIgdId", key.BedIgdId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BedIgdDto GetData(IBedIgdKey key)
    {
        const string sql = """
            SELECT
                aa.BedIgdId, aa.BedIgdName, aa.KamarName,
                aa.BedState, aa.CurrentIgdVisitId,
                aa.OccupyUser, aa.OccupyDateTime,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_BedIgd aa
            WHERE aa.BedIgdId = @BedIgdId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BedIgdId", key.BedIgdId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BedIgdDto>(sql, dp);
    }

    public IEnumerable<BedIgdDto> ListData()
    {
        const string sql = """
            SELECT
                aa.BedIgdId, aa.BedIgdName, aa.KamarName,
                aa.BedState, aa.CurrentIgdVisitId,
                aa.OccupyUser, aa.OccupyDateTime,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_BedIgd aa
            WHERE aa.VodDate = @VodDate
            ORDER BY aa.BedIgdName
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", new DateTime(3000, 1, 1), SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BedIgdDto>(sql, dp);
    }

    public IEnumerable<BedIgdDto> ListAvailable()
    {
        const string sql = """
            SELECT
                aa.BedIgdId, aa.BedIgdName, aa.KamarName,
                aa.BedState, aa.CurrentIgdVisitId,
                aa.OccupyUser, aa.OccupyDateTime,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_BedIgd aa
            WHERE aa.BedState = 'ACTIVE'
              AND (aa.CurrentIgdVisitId = '' OR aa.CurrentIgdVisitId IS NULL)
              AND aa.VodDate = @VodDate
            ORDER BY aa.BedIgdName
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", new DateTime(3000, 1, 1), SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BedIgdDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(BedIgdDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@BedIgdId", dto.BedIgdId, SqlDbType.VarChar);
        dp.AddParam("@BedIgdName", dto.BedIgdName, SqlDbType.VarChar);
        dp.AddParam("@KamarName", dto.KamarName, SqlDbType.VarChar);
        dp.AddParam("@BedState", dto.BedState, SqlDbType.VarChar);
        dp.AddParam("@CurrentIgdVisitId", dto.CurrentIgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@OccupyUser", dto.OccupyUser, SqlDbType.VarChar);
        dp.AddParam("@OccupyDateTime", dto.OccupyDateTime, SqlDbType.DateTime);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
