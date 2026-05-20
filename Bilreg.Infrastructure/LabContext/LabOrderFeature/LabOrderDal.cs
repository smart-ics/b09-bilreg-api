using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public interface ILabOrderDal :
    IInsert<LabOrderDto>,
    IUpdate<LabOrderDto>,
    IDelete<ILabOrderKey>,
    IGetData<LabOrderDto, ILabOrderKey>
{
}

public class LabOrderDal : ILabOrderDal
{
    private readonly DatabaseOptions _opt;

    public LabOrderDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(LabOrderDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_LabOrder (
                OrderId, OrderNo, OrderSource, LabOrderStatus, OwareStatus,
                RegId, PatientId, PatientName, BirthDate, Gender, AgeAtOrder,
                ExecutionRegId, DeferredReason, DeferredUntil,
                BillingTindakanId, BillingLastError,
                CollectedDate, CollectedUserId, CollectionNote,
                ReleasedDate, ReleasedUserId, ReleaseNote,
                CancelledReason, CancelledDate, CancelledUserId,
                TerminationReason, TerminationDate, TerminationUserId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @OrderId, @OrderNo, @OrderSource, @LabOrderStatus, @OwareStatus,
                @RegId, @PatientId, @PatientName, @BirthDate, @Gender, @AgeAtOrder,
                @ExecutionRegId, @DeferredReason, @DeferredUntil,
                @BillingTindakanId, @BillingLastError,
                @CollectedDate, @CollectedUserId, @CollectionNote,
                @ReleasedDate, @ReleasedUserId, @ReleaseNote,
                @CancelledReason, @CancelledDate, @CancelledUserId,
                @TerminationReason, @TerminationDate, @TerminationUserId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(LabOrderDto dto)
    {
        const string sql = """
            UPDATE BILRG_LabOrder
            SET OrderNo = @OrderNo,
                OrderSource = @OrderSource,
                LabOrderStatus = @LabOrderStatus,
                OwareStatus = @OwareStatus,
                RegId = @RegId,
                PatientId = @PatientId,
                PatientName = @PatientName,
                BirthDate = @BirthDate,
                Gender = @Gender,
                AgeAtOrder = @AgeAtOrder,
                ExecutionRegId = @ExecutionRegId,
                DeferredReason = @DeferredReason,
                DeferredUntil = @DeferredUntil,
                BillingTindakanId = @BillingTindakanId,
                BillingLastError = @BillingLastError,
                CollectedDate = @CollectedDate,
                CollectedUserId = @CollectedUserId,
                CollectionNote = @CollectionNote,
                ReleasedDate = @ReleasedDate,
                ReleasedUserId = @ReleasedUserId,
                ReleaseNote = @ReleaseNote,
                CancelledReason = @CancelledReason,
                CancelledDate = @CancelledDate,
                CancelledUserId = @CancelledUserId,
                TerminationReason = @TerminationReason,
                TerminationDate = @TerminationDate,
                TerminationUserId = @TerminationUserId,
                CrtUser = @CrtUser, CrtDate = @CrtDate,
                UpdUser = @UpdUser, UpdDate = @UpdDate,
                VodUser = @VodUser, VodDate = @VodDate
            WHERE OrderId = @OrderId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Delete(ILabOrderKey key)
    {
        const string sql = "DELETE FROM BILRG_LabOrder WHERE OrderId = @OrderId";

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", key.OrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public LabOrderDto GetData(ILabOrderKey key)
    {
        const string sql = """
            SELECT
                aa.OrderId, aa.OrderNo, aa.OrderSource, aa.LabOrderStatus,
                aa.OwareStatus,
                aa.RegId, aa.PatientId, aa.PatientName, aa.BirthDate, aa.Gender, aa.AgeAtOrder,
                aa.ExecutionRegId, aa.DeferredReason, aa.DeferredUntil,
                aa.BillingTindakanId, aa.BillingLastError,
                aa.CollectedDate, aa.CollectedUserId, aa.CollectionNote,
                aa.ReleasedDate, aa.ReleasedUserId, aa.ReleaseNote,
                aa.CancelledReason, aa.CancelledDate, aa.CancelledUserId,
                aa.TerminationReason, aa.TerminationDate, aa.TerminationUserId,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_LabOrder aa
            WHERE aa.OrderId = @OrderId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", key.OrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<LabOrderDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(LabOrderDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", dto.OrderId, SqlDbType.VarChar);
        dp.AddParam("@OrderNo", dto.OrderNo, SqlDbType.VarChar);
        dp.AddParam("@OrderSource", dto.OrderSource, SqlDbType.Int);
        dp.AddParam("@LabOrderStatus", dto.LabOrderStatus, SqlDbType.Int);
        dp.AddParam("@OwareStatus", dto.OwareStatus, SqlDbType.Int);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PatientId", dto.PatientId, SqlDbType.VarChar);
        dp.AddParam("@PatientName", dto.PatientName, SqlDbType.VarChar);
        dp.AddParam("@BirthDate", dto.BirthDate, SqlDbType.DateTime);
        dp.AddParam("@Gender", dto.Gender, SqlDbType.VarChar);
        dp.AddParam("@AgeAtOrder", dto.AgeAtOrder, SqlDbType.Int);
        dp.AddParam("@ExecutionRegId", dto.ExecutionRegId, SqlDbType.VarChar);
        dp.AddParam("@DeferredReason", dto.DeferredReason, SqlDbType.VarChar);
        dp.AddParam("@DeferredUntil", dto.DeferredUntil, SqlDbType.DateTime);
        dp.AddParam("@BillingTindakanId", dto.BillingTindakanId, SqlDbType.VarChar);
        dp.AddParam("@BillingLastError", dto.BillingLastError, SqlDbType.VarChar);
        dp.AddParam("@CollectedDate", dto.CollectedDate, SqlDbType.DateTime);
        dp.AddParam("@CollectedUserId", dto.CollectedUserId, SqlDbType.VarChar);
        dp.AddParam("@CollectionNote", dto.CollectionNote, SqlDbType.VarChar);
        dp.AddParam("@ReleasedDate", dto.ReleasedDate, SqlDbType.DateTime);
        dp.AddParam("@ReleasedUserId", dto.ReleasedUserId, SqlDbType.VarChar);
        dp.AddParam("@ReleaseNote", dto.ReleaseNote, SqlDbType.VarChar);
        dp.AddParam("@CancelledReason", dto.CancelledReason, SqlDbType.VarChar);
        dp.AddParam("@CancelledDate", dto.CancelledDate, SqlDbType.DateTime);
        dp.AddParam("@CancelledUserId", dto.CancelledUserId, SqlDbType.VarChar);
        dp.AddParam("@TerminationReason", dto.TerminationReason, SqlDbType.VarChar);
        dp.AddParam("@TerminationDate", dto.TerminationDate, SqlDbType.DateTime);
        dp.AddParam("@TerminationUserId", dto.TerminationUserId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
