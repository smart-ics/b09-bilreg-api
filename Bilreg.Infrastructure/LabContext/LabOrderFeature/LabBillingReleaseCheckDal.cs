using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public class LabBillingReleaseCheckDal : ILabBillingReleaseCheckDal
{
    private readonly DatabaseOptions _opt;

    public LabBillingReleaseCheckDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BillingReleaseCheckModel check)
    {
        const string sql = """
            INSERT INTO BILRG_LabBillingReleaseCheck (
                CheckId, OrderId, BillingStatus, Message, RequestedByUserId, CheckedAt)
            VALUES (
                @CheckId, @OrderId, @BillingStatus, @Message, @RequestedByUserId, @CheckedAt)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@CheckId", check.CheckId, SqlDbType.VarChar);
        dp.AddParam("@OrderId", check.OrderId, SqlDbType.VarChar);
        dp.AddParam("@BillingStatus", (int)check.BillingStatus, SqlDbType.Int);
        dp.AddParam("@Message", check.Message, SqlDbType.VarChar);
        dp.AddParam("@RequestedByUserId", check.RequestedByUserId, SqlDbType.VarChar);
        dp.AddParam("@CheckedAt", check.CheckedAt, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<BillingReleaseCheckModel> GetLastByOrderId(string orderId)
    {
        const string sql = """
            SELECT TOP 1
                aa.CheckId, aa.OrderId, aa.BillingStatus, aa.Message,
                aa.RequestedByUserId, aa.CheckedAt
            FROM BILRG_LabBillingReleaseCheck aa
            WHERE aa.OrderId = @OrderId
            ORDER BY aa.CheckedAt DESC, aa.CheckId DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", orderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var row = conn.Read<BillingReleaseCheckRowDto>(sql, dp)?.FirstOrDefault();
        if (row is null)
            return MayBe<BillingReleaseCheckModel>.None;

        return MayBe.From(row.ToModel());
    }

    private sealed record BillingReleaseCheckRowDto(
        string CheckId,
        string OrderId,
        int BillingStatus,
        string Message,
        string RequestedByUserId,
        DateTime CheckedAt)
    {
        public BillingReleaseCheckModel ToModel()
            => BillingReleaseCheckModel.Load(
                CheckId,
                OrderId,
                (BillingReleaseStatusEnum)BillingStatus,
                Message,
                RequestedByUserId,
                CheckedAt);
    }
}
