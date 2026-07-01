using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public interface IBilrgMergeRequestDal :
    IInsert<BilrgMergeRequestDto>,
    IUpdate<BilrgMergeRequestDto>,
    IGetData<BilrgMergeRequestDto, IMergeRequestKey>
{
    IEnumerable<BilrgMergeRequestDto> ListPendingByReg(IRegKey regKey);

    IEnumerable<BilrgMergeRequestDto> ListPendingByPatient(string pasienId);
}

public class BilrgMergeRequestDal : IBilrgMergeRequestDal
{
    private readonly DatabaseOptions _opt;

    public BilrgMergeRequestDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BilrgMergeRequestDto model)
    {
        const string sql = """
            INSERT INTO BILRG_MergeRequest (
                MergeRequestId, SourceRegId, TargetRegId, PatientId, Status, Reason,
                ExecutedBy, ExecutedDate, CancelledBy, CancelledDate,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @MergeRequestId, @SourceRegId, @TargetRegId, @PatientId, @Status, @Reason,
                @ExecutedBy, @ExecutedDate, @CancelledBy, @CancelledDate,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(model));
    }

    public void Update(BilrgMergeRequestDto model)
    {
        const string sql = """
            UPDATE BILRG_MergeRequest
            SET SourceRegId = @SourceRegId,
                TargetRegId = @TargetRegId,
                PatientId = @PatientId,
                Status = @Status,
                Reason = @Reason,
                ExecutedBy = CASE WHEN @ExecutedBy <> '' THEN @ExecutedBy ELSE ExecutedBy END,
                ExecutedDate = CASE WHEN @ExecutedBy <> '' THEN @ExecutedDate ELSE ExecutedDate END,
                CancelledBy = CASE WHEN @CancelledBy <> '' THEN @CancelledBy ELSE CancelledBy END,
                CancelledDate = CASE WHEN @CancelledBy <> '' THEN @CancelledDate ELSE CancelledDate END,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE MergeRequestId = @MergeRequestId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(model));
    }

    public BilrgMergeRequestDto GetData(IMergeRequestKey key)
    {
        const string sql = """
            SELECT
                MergeRequestId, SourceRegId, TargetRegId, PatientId, Status, Reason,
                ExecutedBy, ExecutedDate, CancelledBy, CancelledDate,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_MergeRequest
            WHERE MergeRequestId = @MergeRequestId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@MergeRequestId", key.MergeRequestId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BilrgMergeRequestDto>(sql, dp);
    }

    public IEnumerable<BilrgMergeRequestDto> ListPendingByReg(IRegKey regKey)
    {
        const string sql = """
            SELECT
                MergeRequestId, SourceRegId, TargetRegId, PatientId, Status, Reason,
                ExecutedBy, ExecutedDate, CancelledBy, CancelledDate,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_MergeRequest
            WHERE Status = @PendingStatus
              AND (SourceRegId = @RegId OR TargetRegId = @RegId)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PendingStatus", (int)MergeRequestStatusEnum.Pending, SqlDbType.Int);
        dp.AddParam("@RegId", regKey.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BilrgMergeRequestDto>(sql, dp);
    }

    public IEnumerable<BilrgMergeRequestDto> ListPendingByPatient(string pasienId)
    {
        const string sql = """
            SELECT
                MergeRequestId, SourceRegId, TargetRegId, PatientId, Status, Reason,
                ExecutedBy, ExecutedDate, CancelledBy, CancelledDate,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_MergeRequest
            WHERE Status = @PendingStatus
              AND PatientId = @PatientId
              AND (TargetRegId = '' OR TargetRegId IS NULL)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PendingStatus", (int)MergeRequestStatusEnum.Pending, SqlDbType.Int);
        dp.AddParam("@PatientId", pasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BilrgMergeRequestDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(BilrgMergeRequestDto model)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@MergeRequestId", model.MergeRequestId, SqlDbType.VarChar);
        dp.AddParam("@SourceRegId", model.SourceRegId, SqlDbType.VarChar);
        dp.AddParam("@TargetRegId", model.TargetRegId, SqlDbType.VarChar);
        dp.AddParam("@PatientId", model.PatientId, SqlDbType.VarChar);
        dp.AddParam("@Status", model.Status, SqlDbType.Int);
        dp.AddParam("@Reason", model.Reason, SqlDbType.NVarChar);
        dp.AddParam("@ExecutedBy", model.ExecutedBy, SqlDbType.VarChar);
        dp.AddParam("@ExecutedDate", model.ExecutedDate, SqlDbType.DateTime);
        dp.AddParam("@CancelledBy", model.CancelledBy, SqlDbType.VarChar);
        dp.AddParam("@CancelledDate", model.CancelledDate, SqlDbType.DateTime);
        dp.AddParam("@CrtUser", model.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", model.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", model.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", model.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", model.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", model.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
