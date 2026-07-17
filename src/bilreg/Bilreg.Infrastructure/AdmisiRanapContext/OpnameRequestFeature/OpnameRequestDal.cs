using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.OpnameRequestFeature;

public interface IOpnameRequestDal :
    IInsert<OpnameRequestDto>,
    IUpdate<OpnameRequestDto>,
    IGetData<OpnameRequestDto, IOpnameRequestKey>,
    IListData<OpnameRequestDto, OpnameRequestListFilter>
{
}

public class OpnameRequestDal : IOpnameRequestDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private const string SELECT_COLUMNS = """
        aa.OpnameRequestId, aa.OpnameRequestStatus,
        aa.PasienId,
        ISNULL(bb.fs_nm_pasien, '') AS PasienName,
        ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
        ISNULL(bb.fs_jns_kelamin, '') AS Gender,
        aa.DokterId, aa.DokterName, aa.PlannedDate, aa.ClinicalNotes, aa.FulfilledRegId,
        aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
        """;

    private readonly DatabaseOptions _opt;

    public OpnameRequestDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(OpnameRequestDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AdmOpnameRequest (
                OpnameRequestId, OpnameRequestStatus,
                PasienId,
                DokterId, DokterName, PlannedDate, ClinicalNotes, FulfilledRegId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @OpnameRequestId, @OpnameRequestStatus,
                @PasienId,
                @DokterId, @DokterName, @PlannedDate, @ClinicalNotes, @FulfilledRegId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapWriteParams(dto));
    }

    public void Update(OpnameRequestDto dto)
    {
        const string sql = """
            UPDATE BILRG_AdmOpnameRequest
            SET
                OpnameRequestStatus = @OpnameRequestStatus,
                PasienId = @PasienId,
                DokterId = @DokterId,
                DokterName = @DokterName,
                PlannedDate = @PlannedDate,
                ClinicalNotes = @ClinicalNotes,
                FulfilledRegId = @FulfilledRegId,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                OpnameRequestId = @OpnameRequestId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapWriteParams(dto));
    }

    public OpnameRequestDto GetData(IOpnameRequestKey key)
    {
        const string sql = $"""
                            SELECT
                                {SELECT_COLUMNS}
                            FROM BILRG_AdmOpnameRequest aa
                            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
                            WHERE aa.OpnameRequestId = @OpnameRequestId
                            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OpnameRequestId", key.OpnameRequestId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<OpnameRequestDto>(sql, dp);
    }

    public IEnumerable<OpnameRequestDto> ListData(OpnameRequestListFilter filter)
    {
        const string sql = $"""
                            SELECT
                                {SELECT_COLUMNS}
                            FROM BILRG_AdmOpnameRequest aa
                            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
                            WHERE
                                aa.VodDate = @VodDate
                                AND (@Status IS NULL OR aa.OpnameRequestStatus = @Status)
                            ORDER BY aa.CrtDate DESC, aa.OpnameRequestId DESC
                            """;

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@Status", filter.Status.HasValue ? (int)filter.Status.Value : null, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OpnameRequestDto>(sql, dp) ?? [];
    }

    private static DynamicParameters MapWriteParams(OpnameRequestDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@OpnameRequestId", dto.OpnameRequestId, SqlDbType.VarChar);
        dp.AddParam("@OpnameRequestStatus", dto.OpnameRequestStatus, SqlDbType.Int);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@DokterId", dto.DokterId, SqlDbType.VarChar);
        dp.AddParam("@DokterName", dto.DokterName, SqlDbType.VarChar);
        dp.AddParam("@PlannedDate", dto.PlannedDate, SqlDbType.DateTime);
        dp.AddParam("@ClinicalNotes", dto.ClinicalNotes, SqlDbType.VarChar);
        dp.AddParam("@FulfilledRegId", dto.FulfilledRegId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
