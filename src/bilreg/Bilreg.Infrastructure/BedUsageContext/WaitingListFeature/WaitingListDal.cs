using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.WaitingListFeature;

public interface IWaitingListDal :
    IInsert<WaitingListDto>,
    IUpdate<WaitingListDto>,
    IDelete<IWaitingListKey>,
    IGetData<WaitingListDto, IWaitingListKey>
{
    void Delete(string waitingListId);
    WaitingListDto? GetData(string waitingListId);
    IEnumerable<WaitingListDto> ListByReg(string regId);
    bool HasActiveByRegId(string regId);
    WaitingListDto? GetActiveByRegId(string regId);
}

public class WaitingListDal : IWaitingListDal
{
    private const string SelectColumns = """
        aa.WaitingListId, aa.WaitingListStatus, aa.RegId, aa.PasienId,
        ISNULL(bb.fs_nm_pasien, '') AS PasienName,
        ISNULL(CONVERT(VARCHAR(10), bb.fd_tgl_lahir, 120), '3000-01-01') AS TglLahir,
        ISNULL(bb.fs_jns_kelamin, '') AS Gender,
        aa.KelasId, aa.KelasName, aa.BangsalId, aa.BangsalName,
        aa.ArrivalSourceType, aa.ReferringUnitId, aa.ReferringUnitName,
        aa.ReferringDoctorId, aa.ReferringDoctorName,
        aa.ClinicalUrgency, aa.SpecialRequirements,
        aa.PlannedBedId, aa.PlannedBedName,
        aa.PlacedBedId, aa.PlacedPakaiBedId,
        aa.CancellationReason, aa.RerouteReason, aa.PreviousWaitListId,
        aa.Priority,
        aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
        """;

    private readonly DatabaseOptions _opt;

    public WaitingListDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(WaitingListDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_BedWaitingList (
                WaitingListId, WaitingListStatus, RegId, PasienId,
                KelasId, KelasName, BangsalId, BangsalName,
                ArrivalSourceType, ReferringUnitId, ReferringUnitName,
                ReferringDoctorId, ReferringDoctorName,
                ClinicalUrgency, SpecialRequirements,
                PlannedBedId, PlannedBedName,
                PlacedBedId, PlacedPakaiBedId,
                CancellationReason, RerouteReason, PreviousWaitListId,
                Priority,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @WaitingListId, @WaitingListStatus, @RegId, @PasienId,
                @KelasId, @KelasName, @BangsalId, @BangsalName,
                @ArrivalSourceType, @ReferringUnitId, @ReferringUnitName,
                @ReferringDoctorId, @ReferringDoctorName,
                @ClinicalUrgency, @SpecialRequirements,
                @PlannedBedId, @PlannedBedName,
                @PlacedBedId, @PlacedPakaiBedId,
                @CancellationReason, @RerouteReason, @PreviousWaitListId,
                @Priority,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapWriteParams(dto));
    }

    public void Update(WaitingListDto dto)
    {
        const string sql = """
            UPDATE BILRG_BedWaitingList
            SET
                WaitingListStatus = @WaitingListStatus,
                RegId = @RegId,
                PasienId = @PasienId,
                KelasId = @KelasId,
                KelasName = @KelasName,
                BangsalId = @BangsalId,
                BangsalName = @BangsalName,
                ArrivalSourceType = @ArrivalSourceType,
                ReferringUnitId = @ReferringUnitId,
                ReferringUnitName = @ReferringUnitName,
                ReferringDoctorId = @ReferringDoctorId,
                ReferringDoctorName = @ReferringDoctorName,
                ClinicalUrgency = @ClinicalUrgency,
                SpecialRequirements = @SpecialRequirements,
                PlannedBedId = @PlannedBedId,
                PlannedBedName = @PlannedBedName,
                PlacedBedId = @PlacedBedId,
                PlacedPakaiBedId = @PlacedPakaiBedId,
                CancellationReason = @CancellationReason,
                RerouteReason = @RerouteReason,
                PreviousWaitListId = @PreviousWaitListId,
                Priority = @Priority,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                WaitingListId = @WaitingListId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapWriteParams(dto));
    }

    public void Delete(IWaitingListKey key) => Delete(key.WaitingListId);

    public void Delete(string waitingListId)
    {
        const string sql = """
            DELETE FROM BILRG_BedWaitingList
            WHERE WaitingListId = @WaitingListId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@WaitingListId", waitingListId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public WaitingListDto GetData(IWaitingListKey key) => GetData(key.WaitingListId)!;

    public WaitingListDto? GetData(string waitingListId)
    {
        var sql = $"""
            SELECT
                {SelectColumns}
            FROM BILRG_BedWaitingList aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE aa.WaitingListId = @WaitingListId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@WaitingListId", waitingListId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<WaitingListDto>(sql, dp);
    }

    public IEnumerable<WaitingListDto> ListByReg(string regId)
    {
        var sql = $"""
            SELECT
                {SelectColumns}
            FROM BILRG_BedWaitingList aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE aa.RegId = @RegId
            ORDER BY aa.CrtDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<WaitingListDto>(sql, dp) ?? [];
    }

    public bool HasActiveByRegId(string regId) => GetActiveByRegId(regId) is not null;

    public WaitingListDto? GetActiveByRegId(string regId)
    {
        var sql = $"""
            SELECT TOP 1
                {SelectColumns}
            FROM BILRG_BedWaitingList aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                aa.RegId = @RegId
                AND aa.WaitingListStatus IN (@Waiting, @Reserved)
            ORDER BY aa.CrtDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);
        dp.AddParam("@Waiting", (int)WaitingListStatusEnum.Waiting, SqlDbType.Int);
        dp.AddParam("@Reserved", (int)WaitingListStatusEnum.Reserved, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var resultDto = conn.Read<WaitingListDto>(sql, dp);
        return resultDto?.FirstOrDefault();
    }

    private static DynamicParameters MapWriteParams(WaitingListDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@WaitingListId", dto.WaitingListId, SqlDbType.VarChar);
        dp.AddParam("@WaitingListStatus", dto.WaitingListStatus, SqlDbType.Int);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("@KelasName", dto.KelasName, SqlDbType.VarChar);
        dp.AddParam("@BangsalId", dto.BangsalId, SqlDbType.VarChar);
        dp.AddParam("@BangsalName", dto.BangsalName, SqlDbType.VarChar);
        dp.AddParam("@ArrivalSourceType", dto.ArrivalSourceType, SqlDbType.Int);
        dp.AddParam("@ReferringUnitId", dto.ReferringUnitId, SqlDbType.VarChar);
        dp.AddParam("@ReferringUnitName", dto.ReferringUnitName, SqlDbType.VarChar);
        dp.AddParam("@ReferringDoctorId", dto.ReferringDoctorId, SqlDbType.VarChar);
        dp.AddParam("@ReferringDoctorName", dto.ReferringDoctorName, SqlDbType.VarChar);
        dp.AddParam("@ClinicalUrgency", dto.ClinicalUrgency, SqlDbType.Int);
        dp.AddParam("@SpecialRequirements", dto.SpecialRequirements, SqlDbType.VarChar);
        dp.AddParam("@PlannedBedId", dto.PlannedBedId, SqlDbType.VarChar);
        dp.AddParam("@PlannedBedName", dto.PlannedBedName, SqlDbType.VarChar);
        dp.AddParam("@PlacedBedId", dto.PlacedBedId, SqlDbType.VarChar);
        dp.AddParam("@PlacedPakaiBedId", dto.PlacedPakaiBedId, SqlDbType.VarChar);
        dp.AddParam("@CancellationReason", dto.CancellationReason, SqlDbType.VarChar);
        dp.AddParam("@RerouteReason", dto.RerouteReason, SqlDbType.VarChar);
        dp.AddParam("@PreviousWaitListId", dto.PreviousWaitListId, SqlDbType.VarChar);
        dp.AddParam("@Priority", dto.Priority, SqlDbType.Int);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
