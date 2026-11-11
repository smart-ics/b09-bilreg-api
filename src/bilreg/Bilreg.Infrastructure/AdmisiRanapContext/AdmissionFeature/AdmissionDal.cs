using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
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
        aa.RegId, aa.AdmissionStatus, aa.AdmissionSource,
        aa.PasienId,
        ISNULL(bb.fs_nm_pasien, '') AS PasienName,
        ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
        ISNULL(bb.fs_jns_kelamin, '') AS Gender,
        aa.KelasDkId, aa.KelasDkName, aa.BangsalId, aa.BangsalName, aa.AdmissionDate,
        aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
        ISNULL(aa.InpatientOrderId, '-') AS InpatientOrderId
        """;

    private readonly DatabaseOptions _opt;

    public AdmissionDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(AdmissionDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AdmAdmission (
                RegId, AdmissionStatus, AdmissionSource,
                PasienId, InpatientOrderId,
                KelasDkId, KelasDkName, BangsalId, BangsalName, AdmissionDate,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @RegId, @AdmissionStatus, @AdmissionSource,
                @PasienId, @InpatientOrderId,
                @KelasDkId, @KelasDkName, @BangsalId, @BangsalName, @AdmissionDate,
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
                AdmissionSource = @AdmissionSource,
                PasienId = @PasienId,
                InpatientOrderId = @InpatientOrderId,
                KelasDkId = @KelasDkId,
                KelasDkName = @KelasDkName,
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
        var rawSearch = (filter.Search ?? string.Empty).Trim();
        var cleanSearch = Regex.Replace(rawSearch, @"[^\w]", "");

        var sql = $"""
            WITH CombinedAdmissions AS (
                -- 1. All records from BILRG_AdmAdmission
                SELECT
                    aa.RegId,
                    aa.AdmissionStatus,
                    aa.AdmissionSource,
                    aa.PasienId,
                    aa.KelasDkId,
                    aa.KelasDkName,
                    aa.BangsalId,
                    aa.BangsalName,
                    aa.AdmissionDate,
                    aa.CrtUser,
                    aa.CrtDate,
                    aa.UpdUser,
                    aa.UpdDate,
                    aa.VodUser,
                    aa.VodDate,
                    ISNULL(aa.InpatientOrderId, '-') AS InpatientOrderId
                FROM BILRG_AdmAdmission aa
                WHERE aa.VodDate = @VodDate

                UNION ALL

                -- 2. Active registrations in BILRG_RegAktif that do not exist in BILRG_AdmAdmission
                SELECT
                    ra.RegId,
                    0 AS AdmissionStatus,
                    0 AS AdmissionSource,
                    ra.PasienId,
                    '' AS KelasDkId,
                    '' AS KelasDkName,
                    ra.LayananId AS BangsalId,
                    '' AS BangsalName,
                    ra.RegDate AS AdmissionDate,
                    '' AS CrtUser,
                    ra.RegDate AS CrtDate,
                    '' AS UpdUser,
                    '3000-01-01' AS UpdDate,
                    '' AS VodUser,
                    '3000-01-01' AS VodDate,
                    '-' AS InpatientOrderId
                FROM BILRG_RegAktif ra
                WHERE ra.JenisReg = '1'
                  AND NOT EXISTS (SELECT 1 FROM BILRG_AdmAdmission aa2 WHERE aa2.RegId = ra.RegId)
            )
            SELECT TOP 50
                ca.RegId,
                ca.AdmissionStatus,
                ca.AdmissionSource,
                ca.PasienId,
                ISNULL(bb.fs_nm_pasien, '') AS PasienName,
                ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
                ISNULL(bb.fs_jns_kelamin, '') AS Gender,
                ca.KelasDkId,
                ca.KelasDkName,
                ca.BangsalId,
                ca.BangsalName,
                ca.AdmissionDate,
                ca.CrtUser,
                ca.CrtDate,
                ca.UpdUser,
                ca.UpdDate,
                ca.VodUser,
                ca.VodDate,
                ca.InpatientOrderId
            FROM CombinedAdmissions ca
            LEFT JOIN tc_mr bb ON ca.PasienId = bb.fs_mr
            WHERE
                (@Status IS NULL OR ca.AdmissionStatus = @Status)
                AND (@PasienId IS NULL OR @PasienId = '' OR ca.PasienId = @PasienId)
                AND (
                    @Search IS NULL OR @Search = ''
                    OR ca.RegId = @Search
                    OR ca.PasienId = @Search
                    OR (@CleanSearch <> '' AND ca.PasienId LIKE '%' + @CleanSearch + '%')
                    OR (@CleanSearch <> '' AND LEN(@CleanSearch) = 6 AND RIGHT(ca.PasienId, 6) = @CleanSearch)
                    OR bb.fs_nm_pasien LIKE '%' + @Search + '%'
                )
            ORDER BY ca.CrtDate DESC, ca.RegId DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@Status", filter.Status.HasValue ? (int)filter.Status.Value : null, SqlDbType.Int);
        dp.AddParam("@PasienId", filter.PasienId ?? "", SqlDbType.VarChar);
        dp.AddParam("@Search", rawSearch, SqlDbType.VarChar);
        dp.AddParam("@CleanSearch", cleanSearch, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AdmissionDto>(sql, dp) ?? [];
    }

    private static DynamicParameters MapWriteParams(AdmissionDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@AdmissionStatus", dto.AdmissionStatus, SqlDbType.Int);
        dp.AddParam("@AdmissionSource", dto.AdmissionSource, SqlDbType.Int);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@InpatientOrderId", string.IsNullOrWhiteSpace(dto.InpatientOrderId) ? "-" : dto.InpatientOrderId, SqlDbType.VarChar);
        dp.AddParam("@KelasDkId", dto.KelasDkId, SqlDbType.VarChar);
        dp.AddParam("@KelasDkName", dto.KelasDkName, SqlDbType.VarChar);
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
