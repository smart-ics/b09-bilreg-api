using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IPreOpClearanceDal :
    IInsert<PreOpClearanceDto>,
    IUpdate<PreOpClearanceDto>,
    IGetData<PreOpClearanceDto, IPreOpClearanceKey>
{
    PreOpClearanceDto? GetData(string clearanceId);
    PreOpClearanceDto? GetLatestByScheduleOpId(string scheduleOpId);
    int CountByScheduleOpId(string scheduleOpId);
}

public class PreOpClearanceDal : IPreOpClearanceDal
{
    private readonly DatabaseOptions _opt;

    public PreOpClearanceDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PreOpClearanceDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_PreOpClearance (
                ClearanceId, ScheduleOpId, OrderOpId, RegId, NoUrut, ClearanceStatus,
                VerifikatorPpaId, DokterPjId, CompletedAt,
                DokterOverrideId, AlasanOverride, AlasanNotCleared,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            )
            VALUES (
                @ClearanceId, @ScheduleOpId, @OrderOpId, @RegId, @NoUrut, @ClearanceStatus,
                @VerifikatorPpaId, @DokterPjId, @CompletedAt,
                @DokterOverrideId, @AlasanOverride, @AlasanNotCleared,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ClearanceId", dto.ClearanceId, SqlDbType.VarChar);
        dp.AddParam("@ScheduleOpId", dto.ScheduleOpId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
        dp.AddParam("@ClearanceStatus", dto.ClearanceStatus, SqlDbType.Int);
        dp.AddParam("@VerifikatorPpaId", dto.VerifikatorPpaId, SqlDbType.VarChar);
        dp.AddParam("@DokterPjId", dto.DokterPjId, SqlDbType.VarChar);
        dp.AddParam("@CompletedAt", dto.CompletedAt, SqlDbType.DateTime);
        dp.AddParam("@DokterOverrideId", dto.DokterOverrideId, SqlDbType.VarChar);
        dp.AddParam("@AlasanOverride", dto.AlasanOverride, SqlDbType.VarChar);
        dp.AddParam("@AlasanNotCleared", dto.AlasanNotCleared, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PreOpClearanceDto dto)
    {
        const string sql = """
            UPDATE
                BILRG_PreOpClearance
            SET
                ScheduleOpId = @ScheduleOpId,
                OrderOpId = @OrderOpId,
                RegId = @RegId,
                NoUrut = @NoUrut,
                ClearanceStatus = @ClearanceStatus,
                VerifikatorPpaId = @VerifikatorPpaId,
                DokterPjId = @DokterPjId,
                CompletedAt = @CompletedAt,
                DokterOverrideId = @DokterOverrideId,
                AlasanOverride = @AlasanOverride,
                AlasanNotCleared = @AlasanNotCleared,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                ClearanceId = @ClearanceId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ClearanceId", dto.ClearanceId, SqlDbType.VarChar);
        dp.AddParam("@ScheduleOpId", dto.ScheduleOpId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
        dp.AddParam("@ClearanceStatus", dto.ClearanceStatus, SqlDbType.Int);
        dp.AddParam("@VerifikatorPpaId", dto.VerifikatorPpaId, SqlDbType.VarChar);
        dp.AddParam("@DokterPjId", dto.DokterPjId, SqlDbType.VarChar);
        dp.AddParam("@CompletedAt", dto.CompletedAt, SqlDbType.DateTime);
        dp.AddParam("@DokterOverrideId", dto.DokterOverrideId, SqlDbType.VarChar);
        dp.AddParam("@AlasanOverride", dto.AlasanOverride, SqlDbType.VarChar);
        dp.AddParam("@AlasanNotCleared", dto.AlasanNotCleared, SqlDbType.VarChar);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PreOpClearanceDto GetData(IPreOpClearanceKey key)
    {
        return GetData(key.ClearanceId)!;
    }

    public PreOpClearanceDto? GetData(string clearanceId)
    {
        const string sql = """
            SELECT
                aa.*
            FROM
                BILRG_PreOpClearance aa
            WHERE
                aa.ClearanceId = @ClearanceId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ClearanceId", clearanceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PreOpClearanceDto>(sql, dp);
    }

    public PreOpClearanceDto? GetLatestByScheduleOpId(string scheduleOpId)
    {
        const string sql = """
            SELECT TOP 1
                aa.*
            FROM
                BILRG_PreOpClearance aa
            WHERE
                aa.ScheduleOpId = @ScheduleOpId
                AND aa.VodDate = '3000-01-01'
            ORDER BY
                aa.NoUrut DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ScheduleOpId", scheduleOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PreOpClearanceDto>(sql, dp);
    }

    public int CountByScheduleOpId(string scheduleOpId)
    {
        const string sql = """
            SELECT
                COUNT(1)
            FROM
                BILRG_PreOpClearance aa
            WHERE
                aa.ScheduleOpId = @ScheduleOpId
                AND aa.VodDate = '3000-01-01'
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ScheduleOpId", scheduleOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql, dp);
    }
}
