using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabExternalFeature;

public class LabExternalDal : ILabExternalDal
{
    private const string SelectColumns = """
        RegExternalId, ExternalMrNo, PatientName, Nik, BirthDate, Gender, Phone, Address,
        ReferralType, ReferralDoctor, ReferralOrigin, ClinicalNotes,
        BillingAccountId, Status,
        CancelReason, CancelledUserId, CancelledDate,
        CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
        """;

    private readonly DatabaseOptions _opt;

    public LabExternalDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(LabExternalDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_LabExternal (
                RegExternalId, ExternalMrNo, PatientName, Nik, BirthDate, Gender, Phone, Address,
                ReferralType, ReferralDoctor, ReferralOrigin, ClinicalNotes,
                BillingAccountId, Status,
                CancelReason, CancelledUserId, CancelledDate,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @RegExternalId, @ExternalMrNo, @PatientName, @Nik, @BirthDate, @Gender, @Phone, @Address,
                @ReferralType, @ReferralDoctor, @ReferralOrigin, @ClinicalNotes,
                @BillingAccountId, @Status,
                @CancelReason, @CancelledUserId, @CancelledDate,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(LabExternalDto dto)
    {
        const string sql = """
            UPDATE BILRG_LabExternal
            SET ExternalMrNo = @ExternalMrNo,
                PatientName = @PatientName,
                Nik = @Nik,
                BirthDate = @BirthDate,
                Gender = @Gender,
                Phone = @Phone,
                Address = @Address,
                ReferralType = @ReferralType,
                ReferralDoctor = @ReferralDoctor,
                ReferralOrigin = @ReferralOrigin,
                ClinicalNotes = @ClinicalNotes,
                BillingAccountId = @BillingAccountId,
                Status = @Status,
                CancelReason = @CancelReason,
                CancelledUserId = @CancelledUserId,
                CancelledDate = @CancelledDate,
                CrtUser = @CrtUser,
                CrtDate = @CrtDate,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE RegExternalId = @RegExternalId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Delete(ILabExternalKey key)
    {
        const string sql = "DELETE FROM BILRG_LabExternal WHERE RegExternalId = @RegExternalId";

        var dp = new DynamicParameters();
        dp.AddParam("@RegExternalId", key.RegExternalId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public LabExternalDto GetData(ILabExternalKey key)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabExternal
            WHERE RegExternalId = @RegExternalId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegExternalId", key.RegExternalId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<LabExternalDto>(sql, dp);
    }

    public LabExternalDto? GetByExternalMrNo(string externalMrNo)
    {
        var sql = $"""
            SELECT TOP 1 {SelectColumns}
            FROM BILRG_LabExternal
            WHERE ExternalMrNo = @ExternalMrNo
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ExternalMrNo", externalMrNo, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabExternalDto>(sql, dp).FirstOrDefault();
    }

    public IEnumerable<LabExternalDto> ListData(string? searchTerm, DateTime? date1, DateTime? date2, int? status)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabExternal
            WHERE 1 = 1
            """;

        var dp = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql += """
                 AND (
                     ExternalMrNo LIKE @SearchPattern
                     OR PatientName LIKE @SearchPattern
                     OR Nik LIKE @SearchPattern
                     OR Phone LIKE @SearchPattern
                 )
                """;
            dp.AddParam("@SearchPattern", $"%{searchTerm.Trim()}%", SqlDbType.VarChar);
        }

        if (date1.HasValue)
        {
            sql += " AND CrtDate >= @Date1";
            dp.AddParam("@Date1", date1.Value.Date, SqlDbType.DateTime);
        }

        if (date2.HasValue)
        {
            sql += " AND CrtDate < @Date2End";
            dp.AddParam("@Date2End", date2.Value.Date.AddDays(1), SqlDbType.DateTime);
        }

        if (status.HasValue)
        {
            sql += " AND Status = @Status";
            dp.AddParam("@Status", status.Value, SqlDbType.Int);
        }

        sql += " ORDER BY CrtDate DESC, RegExternalId DESC";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabExternalDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(LabExternalDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@RegExternalId", dto.RegExternalId, SqlDbType.VarChar);
        dp.AddParam("@ExternalMrNo", dto.ExternalMrNo, SqlDbType.VarChar);
        dp.AddParam("@PatientName", dto.PatientName, SqlDbType.VarChar);
        dp.AddParam("@Nik", dto.Nik, SqlDbType.VarChar);
        dp.AddParam("@BirthDate", dto.BirthDate == default ? new DateTime(3000, 1, 1) : dto.BirthDate, SqlDbType.DateTime);
        dp.AddParam("@Gender", dto.Gender, SqlDbType.VarChar);
        dp.AddParam("@Phone", dto.Phone, SqlDbType.VarChar);
        dp.AddParam("@Address", dto.Address, SqlDbType.VarChar);
        dp.AddParam("@ReferralType", dto.ReferralType, SqlDbType.Int);
        dp.AddParam("@ReferralDoctor", dto.ReferralDoctor, SqlDbType.VarChar);
        dp.AddParam("@ReferralOrigin", dto.ReferralOrigin, SqlDbType.VarChar);
        dp.AddParam("@ClinicalNotes", dto.ClinicalNotes, SqlDbType.VarChar);
        dp.AddParam("@BillingAccountId", dto.BillingAccountId, SqlDbType.VarChar);
        dp.AddParam("@Status", dto.Status, SqlDbType.Int);
        dp.AddParam("@CancelReason", dto.CancelReason, SqlDbType.VarChar);
        dp.AddParam("@CancelledUserId", dto.CancelledUserId, SqlDbType.VarChar);
        dp.AddParam("@CancelledDate", dto.CancelledDate == default ? new DateTime(3000, 1, 1) : dto.CancelledDate, SqlDbType.DateTime);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate == default ? new DateTime(3000, 1, 1) : dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate == default ? new DateTime(3000, 1, 1) : dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate == default ? new DateTime(3000, 1, 1) : dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
