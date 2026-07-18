using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.WaitingListFeature;

public interface IWaitingListDal :
    IInsert<WaitingListDto>,
    IUpdate<WaitingListDto>,
    IGetData<WaitingListDto, IWaitingListKey>
{
    bool HasActiveByRegId(string regId);
    WaitingListDto? GetActiveByRegId(string regId);
}

public class WaitingListDal : IWaitingListDal
{
    private const string SelectColumns = """
        aa.WaitingListId, aa.WaitingListStatus, aa.RegId,
        aa.PasienId,
        ISNULL(bb.fs_nm_pasien, '') AS PasienName,
        ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
        ISNULL(bb.fs_jns_kelamin, '') AS Gender,
        aa.KelasId, aa.KelasName, aa.BangsalId, aa.BangsalName, aa.Priority,
        aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
        """;

    private readonly DatabaseOptions _opt;

    public WaitingListDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(WaitingListDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_BedWaitingList (
                WaitingListId, WaitingListStatus, RegId,
                PasienId,
                KelasId, KelasName, BangsalId, BangsalName, Priority,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @WaitingListId, @WaitingListStatus, @RegId,
                @PasienId,
                @KelasId, @KelasName, @BangsalId, @BangsalName, @Priority,
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

    public WaitingListDto GetData(IWaitingListKey key)
    {
        var sql = $"""
            SELECT
                {SelectColumns}
            FROM BILRG_BedWaitingList aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE aa.WaitingListId = @WaitingListId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@WaitingListId", key.WaitingListId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<WaitingListDto>(sql, dp);
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
                AND aa.WaitingListStatus IN (@Waiting, @Accepted)
            ORDER BY aa.CrtDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);
        dp.AddParam("@Waiting", (int)WaitingListStatusEnum.Waiting, SqlDbType.Int);
        dp.AddParam("@Accepted", (int)WaitingListStatusEnum.Accepted, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var resultDto = conn.Read<WaitingListDto>(sql, dp);
        var result = resultDto?.FirstOrDefault();
        return result;
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
