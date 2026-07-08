using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;

public interface IAdmissionDal :
    IInsert<AdmissionDto>,
    IUpdate<AdmissionDto>,
    IGetData<AdmissionDto, IRegKey>,
    IListData<AdmissionDto, AdmissionListFilter>
{
}

public class AdmissionDal : IAdmissionDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private const string SELECT_COLUMNS = """
        aa.RegId, aa.AdmissionStatus,
        aa.PasienId,
        ISNULL(bb.fs_nm_pasien, '') AS PasienName,
        ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
        ISNULL(bb.fs_jns_kelamin, '') AS Gender,
        aa.OpnameRequestId, aa.ReservationId,
        aa.KelasId, aa.KelasName, aa.BangsalId, aa.BangsalName, aa.AdmissionDate,
        aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
        """;

    private readonly DatabaseOptions _opt;

    public AdmissionDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(AdmissionDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AdmAdmission (
                RegId, AdmissionStatus,
                PasienId,
                OpnameRequestId, ReservationId,
                KelasId, KelasName, BangsalId, BangsalName, AdmissionDate,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @RegId, @AdmissionStatus,
                @PasienId,
                @OpnameRequestId, @ReservationId,
                @KelasId, @KelasName, @BangsalId, @BangsalName, @AdmissionDate,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapWriteParams(dto));
    }

    public void Update(AdmissionDto dto)
    {
        const string sql = """
            UPDATE BILRG_AdmAdmission
            SET
                AdmissionStatus = @AdmissionStatus,
                PasienId = @PasienId,
                OpnameRequestId = @OpnameRequestId,
                ReservationId = @ReservationId,
                KelasId = @KelasId,
                KelasName = @KelasName,
                BangsalId = @BangsalId,
                BangsalName = @BangsalName,
                AdmissionDate = @AdmissionDate,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                RegId = @RegId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapWriteParams(dto));
    }

    public AdmissionDto GetData(IRegKey key)
    {
        var sql = $"""
            SELECT
                {SELECT_COLUMNS}
            FROM BILRG_AdmAdmission aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE aa.RegId = @RegId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<AdmissionDto>(sql, dp);
    }

    public IEnumerable<AdmissionDto> ListData(AdmissionListFilter filter)
    {
        var sql = $"""
            SELECT
                {SELECT_COLUMNS}
            FROM BILRG_AdmAdmission aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                aa.VodDate = @VodDate
                AND (@Status IS NULL OR aa.AdmissionStatus = @Status)
                AND (@PasienId IS NULL OR @PasienId = '' OR aa.PasienId = @PasienId)
            ORDER BY aa.CrtDate DESC, aa.RegId DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@Status", filter.Status.HasValue ? (int)filter.Status.Value : null, SqlDbType.Int);
        dp.AddParam("@PasienId", filter.PasienId ?? "", SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AdmissionDto>(sql, dp) ?? [];
    }

    private static DynamicParameters MapWriteParams(AdmissionDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@AdmissionStatus", dto.AdmissionStatus, SqlDbType.Int);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@OpnameRequestId", dto.OpnameRequestId, SqlDbType.VarChar);
        dp.AddParam("@ReservationId", dto.ReservationId, SqlDbType.VarChar);
        dp.AddParam("@KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("@KelasName", dto.KelasName, SqlDbType.VarChar);
        dp.AddParam("@BangsalId", dto.BangsalId, SqlDbType.VarChar);
        dp.AddParam("@BangsalName", dto.BangsalName, SqlDbType.VarChar);
        dp.AddParam("@AdmissionDate", dto.AdmissionDate, SqlDbType.DateTime);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
