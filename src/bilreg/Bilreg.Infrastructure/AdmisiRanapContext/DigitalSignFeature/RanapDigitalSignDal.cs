using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.DigitalSignFeature;

public interface IRanapDigitalSignDal :
    IInsert<RanapDigitalSignDto>,
    IUpdate<RanapDigitalSignDto>,
    IGetData<RanapDigitalSignDto, IRanapDigitalSignKey>
{
    RanapDigitalSignDto? GetByRegDokumen(string regId, string dokumenId);
    IEnumerable<RanapDigitalSignDto> ListByRegId(string regId);
}

public class RanapDigitalSignDal : IRanapDigitalSignDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private const string SELECT_COLUMNS = """
        aa.SigningRequestId, aa.RegId, aa.HisReference, aa.DokumenId,
        aa.PasienId,
        ISNULL(bb.fs_nm_pasien, '') AS PasienName,
        ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
        ISNULL(bb.fs_jns_kelamin, '') AS Gender,
        aa.SignerId, aa.FileName,
        aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
        """;

    private readonly DatabaseOptions _opt;

    public RanapDigitalSignDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(RanapDigitalSignDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AdmDigitalSign (
                SigningRequestId, RegId, HisReference, DokumenId,
                PasienId,
                SignerId, FileName,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @SigningRequestId, @RegId, @HisReference, @DokumenId,
                @PasienId,
                @SignerId, @FileName,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapWriteParams(dto));
    }

    public void Update(RanapDigitalSignDto dto)
    {
        const string sql = """
            UPDATE BILRG_AdmDigitalSign
            SET
                RegId = @RegId,
                HisReference = @HisReference,
                DokumenId = @DokumenId,
                PasienId = @PasienId,
                SignerId = @SignerId,
                FileName = @FileName,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                SigningRequestId = @SigningRequestId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapWriteParams(dto));
    }

    public RanapDigitalSignDto GetData(IRanapDigitalSignKey key)
    {
        var sql = $"""
            SELECT
                {SELECT_COLUMNS}
            FROM BILRG_AdmDigitalSign aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE aa.SigningRequestId = @SigningRequestId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@SigningRequestId", key.SigningRequestId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RanapDigitalSignDto>(sql, dp);
    }

    public RanapDigitalSignDto? GetByRegDokumen(string regId, string dokumenId)
    {
        var sql = $"""
            SELECT TOP 1
                {SELECT_COLUMNS}
            FROM BILRG_AdmDigitalSign aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                aa.RegId = @RegId
                AND aa.DokumenId = @DokumenId
                AND aa.VodDate = @VodDate
            ORDER BY aa.CrtDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);
        dp.AddParam("@DokumenId", dokumenId, SqlDbType.VarChar);
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RanapDigitalSignDto>(sql, dp);
    }

    public IEnumerable<RanapDigitalSignDto> ListByRegId(string regId)
    {
        var sql = $"""
            SELECT
                {SELECT_COLUMNS}
            FROM BILRG_AdmDigitalSign aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                aa.RegId = @RegId
                AND aa.VodDate = @VodDate
            ORDER BY aa.CrtDate ASC, aa.DokumenId ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RanapDigitalSignDto>(sql, dp) ?? [];
    }

    private static DynamicParameters MapWriteParams(RanapDigitalSignDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@SigningRequestId", dto.SigningRequestId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@HisReference", dto.HisReference, SqlDbType.VarChar);
        dp.AddParam("@DokumenId", dto.DokumenId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@SignerId", dto.SignerId, SqlDbType.VarChar);
        dp.AddParam("@FileName", dto.FileName, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
