using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

public interface ILabResultDocumentDal :
    IInsert<LabResultDocumentDto>,
    IUpdate<LabResultDocumentDto>,
    IDelete<ILabResultDocumentKey>
{
    LabResultDocumentDto? GetData(ILabResultDocumentKey key);
    LabResultDocumentDto? GetByOrderId(string orderId);
}

public class LabResultDocumentDal : ILabResultDocumentDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private readonly DatabaseOptions _opt;

    public LabResultDocumentDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(LabResultDocumentDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_LabResultDocument (
                ResultDocumentId, OrderId, VersionNo, IsCurrentVersion,
                ResultSource, ResultStatus,
                RecordedDate, RecordedUserId, VerifiedDate, VerifiedUserId,
                AmendmentReason, AmendedDate, AmendedUserId, PreviousVersionId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @ResultDocumentId, @OrderId, @VersionNo, @IsCurrentVersion,
                @ResultSource, @ResultStatus,
                @RecordedDate, @RecordedUserId, @VerifiedDate, @VerifiedUserId,
                @AmendmentReason, @AmendedDate, @AmendedUserId, @PreviousVersionId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(LabResultDocumentDto dto)
    {
        const string sql = """
            UPDATE BILRG_LabResultDocument
            SET OrderId = @OrderId,
                VersionNo = @VersionNo,
                IsCurrentVersion = @IsCurrentVersion,
                ResultSource = @ResultSource,
                ResultStatus = @ResultStatus,
                RecordedDate = @RecordedDate,
                RecordedUserId = @RecordedUserId,
                VerifiedDate = @VerifiedDate,
                VerifiedUserId = @VerifiedUserId,
                AmendmentReason = @AmendmentReason,
                AmendedDate = @AmendedDate,
                AmendedUserId = @AmendedUserId,
                PreviousVersionId = @PreviousVersionId,
                CrtUser = @CrtUser, CrtDate = @CrtDate,
                UpdUser = @UpdUser, UpdDate = @UpdDate,
                VodUser = @VodUser, VodDate = @VodDate
            WHERE ResultDocumentId = @ResultDocumentId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Delete(ILabResultDocumentKey key)
    {
        const string sql = "DELETE FROM BILRG_LabResultDocument WHERE ResultDocumentId = @ResultDocumentId";

        var dp = new DynamicParameters();
        dp.AddParam("@ResultDocumentId", key.ResultDocumentId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public LabResultDocumentDto? GetData(ILabResultDocumentKey key)
    {
        const string sql = """
            SELECT
                aa.ResultDocumentId, aa.OrderId, aa.VersionNo, aa.IsCurrentVersion,
                aa.ResultSource, aa.ResultStatus,
                aa.RecordedDate, aa.RecordedUserId, aa.VerifiedDate, aa.VerifiedUserId,
                aa.AmendmentReason, aa.AmendedDate, aa.AmendedUserId, aa.PreviousVersionId,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_LabResultDocument aa
            WHERE aa.ResultDocumentId = @ResultDocumentId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ResultDocumentId", key.ResultDocumentId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<LabResultDocumentDto>(sql, dp);
    }

    public LabResultDocumentDto? GetByOrderId(string orderId)
    {
        const string sql = """
            SELECT
                aa.ResultDocumentId, aa.OrderId, aa.VersionNo, aa.IsCurrentVersion,
                aa.ResultSource, aa.ResultStatus,
                aa.RecordedDate, aa.RecordedUserId, aa.VerifiedDate, aa.VerifiedUserId,
                aa.AmendmentReason, aa.AmendedDate, aa.AmendedUserId, aa.PreviousVersionId,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_LabResultDocument aa
            WHERE aa.OrderId = @OrderId
              AND aa.IsCurrentVersion = 1
              AND aa.VodDate = @VodDate
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", orderId, SqlDbType.VarChar);
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<LabResultDocumentDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(LabResultDocumentDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@ResultDocumentId", dto.ResultDocumentId, SqlDbType.VarChar);
        dp.AddParam("@OrderId", dto.OrderId, SqlDbType.VarChar);
        dp.AddParam("@VersionNo", dto.VersionNo, SqlDbType.Int);
        dp.AddParam("@IsCurrentVersion", dto.IsCurrentVersion, SqlDbType.Bit);
        dp.AddParam("@ResultSource", dto.ResultSource, SqlDbType.Int);
        dp.AddParam("@ResultStatus", dto.ResultStatus, SqlDbType.Int);
        dp.AddParam("@RecordedDate", dto.RecordedDate, SqlDbType.DateTime);
        dp.AddParam("@RecordedUserId", dto.RecordedUserId, SqlDbType.VarChar);
        dp.AddParam("@VerifiedDate", dto.VerifiedDate, SqlDbType.DateTime);
        dp.AddParam("@VerifiedUserId", dto.VerifiedUserId, SqlDbType.VarChar);
        dp.AddParam("@AmendmentReason", dto.AmendmentReason, SqlDbType.VarChar);
        dp.AddParam("@AmendedDate", dto.AmendedDate, SqlDbType.DateTime);
        dp.AddParam("@AmendedUserId", dto.AmendedUserId, SqlDbType.VarChar);
        dp.AddParam("@PreviousVersionId", dto.PreviousVersionId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
