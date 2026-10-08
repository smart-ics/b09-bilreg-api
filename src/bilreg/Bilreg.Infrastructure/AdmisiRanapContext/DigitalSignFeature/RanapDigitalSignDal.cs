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
    RanapDigitalSignDto? GetByExternalDoc(string regId, string dokumenId, string externalDocumentId);
    RanapDigitalSignDto? GetByOftaDocId(string oftaDocId);
    IEnumerable<RanapDigitalSignDto> ListByRegId(string regId);
    IEnumerable<RanapDigitalSignDto> ListPendingArchive(int limit);
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
        aa.SignerId, aa.FileName, aa.PatientSignState,
        aa.OftaDocId, aa.OftaDocState, aa.OftaSignState,
        aa.OfficerRef, aa.OfficerEmail, aa.OfficerName,
        aa.ExternalDocumentId, aa.SignedDocUrl,
        aa.IsArchived, aa.ArchiveId, aa.ArchiveDate,
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
                SignerId, FileName, PatientSignState,
                OftaDocId, OftaDocState, OftaSignState,
                OfficerRef, OfficerEmail, OfficerName,
                ExternalDocumentId, SignedDocUrl,
                IsArchived, ArchiveId, ArchiveDate,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @SigningRequestId, @RegId, @HisReference, @DokumenId,
                @PasienId,
                @SignerId, @FileName, @PatientSignState,
                @OftaDocId, @OftaDocState, @OftaSignState,
                @OfficerRef, @OfficerEmail, @OfficerName,
                @ExternalDocumentId, @SignedDocUrl,
                @IsArchived, @ArchiveId, @ArchiveDate,
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
                PatientSignState = @PatientSignState,
                OftaDocId = @OftaDocId,
                OftaDocState = @OftaDocState,
                OftaSignState = @OftaSignState,
                OfficerRef = @OfficerRef,
                OfficerEmail = @OfficerEmail,
                OfficerName = @OfficerName,
                ExternalDocumentId = @ExternalDocumentId,
                SignedDocUrl = @SignedDocUrl,
                IsArchived = @IsArchived,
                ArchiveId = @ArchiveId,
                ArchiveDate = @ArchiveDate,
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

    public RanapDigitalSignDto? GetByExternalDoc(string regId, string dokumenId, string externalDocumentId)
    {
        var sql = $"""
            SELECT TOP 1
                {SELECT_COLUMNS}
            FROM BILRG_AdmDigitalSign aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                aa.RegId = @RegId
                AND aa.DokumenId = @DokumenId
                AND aa.ExternalDocumentId = @ExternalDocumentId
                AND aa.VodDate = @VodDate
            ORDER BY aa.CrtDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);
        dp.AddParam("@DokumenId", dokumenId, SqlDbType.VarChar);
        dp.AddParam("@ExternalDocumentId", externalDocumentId, SqlDbType.VarChar);
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RanapDigitalSignDto>(sql, dp);
    }

    public RanapDigitalSignDto? GetByOftaDocId(string oftaDocId)
    {
        var sql = $"""
            SELECT TOP 1
                {SELECT_COLUMNS}
            FROM BILRG_AdmDigitalSign aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                aa.OftaDocId = @OftaDocId
                AND aa.VodDate = @VodDate
            ORDER BY aa.CrtDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OftaDocId", oftaDocId, SqlDbType.VarChar);
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

    public IEnumerable<RanapDigitalSignDto> ListPendingArchive(int limit)
    {
        var topClause = limit > 0 ? $"TOP ({limit})" : "TOP (50)";
        var sql = $"""
            SELECT {topClause}
                {SELECT_COLUMNS}
            FROM BILRG_AdmDigitalSign aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                aa.IsArchived = 0
                AND (aa.OftaSignState = 'Signed' OR aa.OftaSignState = 'SIGNED')
                AND (aa.PatientSignState = 'Signed' OR aa.PatientSignState = 'SIGNED')
                AND aa.VodDate = @VodDate
            ORDER BY aa.CrtDate ASC
            """;

        var dp = new DynamicParameters();
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
        dp.AddParam("@PatientSignState", dto.PatientSignState, SqlDbType.VarChar);
        dp.AddParam("@OftaDocId", dto.OftaDocId, SqlDbType.VarChar);
        dp.AddParam("@OftaDocState", dto.OftaDocState, SqlDbType.VarChar);
        dp.AddParam("@OftaSignState", dto.OftaSignState, SqlDbType.VarChar);
        dp.AddParam("@OfficerRef", dto.OfficerRef, SqlDbType.VarChar);
        dp.AddParam("@OfficerEmail", dto.OfficerEmail, SqlDbType.VarChar);
        dp.AddParam("@OfficerName", dto.OfficerName, SqlDbType.VarChar);
        dp.AddParam("@ExternalDocumentId", dto.ExternalDocumentId, SqlDbType.VarChar);
        dp.AddParam("@SignedDocUrl", dto.SignedDocUrl, SqlDbType.VarChar);
        dp.AddParam("@IsArchived", dto.IsArchived, SqlDbType.Bit);
        dp.AddParam("@ArchiveId", dto.ArchiveId, SqlDbType.VarChar);
        dp.AddParam("@ArchiveDate", dto.ArchiveDate, SqlDbType.DateTime);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
