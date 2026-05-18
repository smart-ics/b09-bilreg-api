using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.LabContext.LabOwareFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabOwareFeature;

public interface ILabOwareOutboundQueueDal :
    IInsert<LabOwareOutboundQueueDto>,
    IUpdate<LabOwareOutboundQueueDto>,
    IGetData<LabOwareOutboundQueueDto, ILabOwareOutboundQueueKey>
{
    IEnumerable<LabOwareOutboundQueueDto> ListProcessable(int batchSize);
}

public class LabOwareOutboundQueueDal : ILabOwareOutboundQueueDal
{
    private readonly DatabaseOptions _opt;

    public LabOwareOutboundQueueDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(LabOwareOutboundQueueDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_LabOwareOutboundQueue (
                QueueId, OrderId, MessageType, PayloadJson, QueueStatus,
                RetryCount, LastRetryDate, ProcessedDate, LastError, CrtDate)
            VALUES (
                @QueueId, @OrderId, @MessageType, @PayloadJson, @QueueStatus,
                @RetryCount, @LastRetryDate, @ProcessedDate, @LastError, @CrtDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(LabOwareOutboundQueueDto dto)
    {
        const string sql = """
            UPDATE BILRG_LabOwareOutboundQueue SET
                OrderId = @OrderId,
                MessageType = @MessageType,
                PayloadJson = @PayloadJson,
                QueueStatus = @QueueStatus,
                RetryCount = @RetryCount,
                LastRetryDate = @LastRetryDate,
                ProcessedDate = @ProcessedDate,
                LastError = @LastError
            WHERE QueueId = @QueueId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public LabOwareOutboundQueueDto GetData(ILabOwareOutboundQueueKey key)
    {
        const string sql = """
            SELECT
                aa.QueueId, aa.OrderId, aa.MessageType, aa.PayloadJson, aa.QueueStatus,
                aa.RetryCount, aa.LastRetryDate, aa.ProcessedDate, aa.LastError, aa.CrtDate
            FROM BILRG_LabOwareOutboundQueue aa
            WHERE aa.QueueId = @QueueId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@QueueId", key.QueueId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<LabOwareOutboundQueueDto>(sql, dp);
    }

    public IEnumerable<LabOwareOutboundQueueDto> ListProcessable(int batchSize)
    {
        const string sql = """
            SELECT TOP (@BatchSize)
                aa.QueueId, aa.OrderId, aa.MessageType, aa.PayloadJson, aa.QueueStatus,
                aa.RetryCount, aa.LastRetryDate, aa.ProcessedDate, aa.LastError, aa.CrtDate
            FROM BILRG_LabOwareOutboundQueue aa
            WHERE aa.QueueStatus IN (@Pending, @Failed)
            ORDER BY aa.CrtDate ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BatchSize", batchSize, SqlDbType.Int);
        dp.AddParam("@Pending", (int)LabOwareQueueStatusEnum.Pending, SqlDbType.Int);
        dp.AddParam("@Failed", (int)LabOwareQueueStatusEnum.Failed, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<LabOwareOutboundQueueDto>(sql, dp).ToList();
    }

    private static DynamicParameters BuildParams(LabOwareOutboundQueueDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@QueueId", dto.QueueId, SqlDbType.VarChar);
        dp.AddParam("@OrderId", dto.OrderId, SqlDbType.VarChar);
        dp.AddParam("@MessageType", dto.MessageType, SqlDbType.VarChar);
        dp.AddParam("@PayloadJson", dto.PayloadJson, SqlDbType.NVarChar);
        dp.AddParam("@QueueStatus", dto.QueueStatus, SqlDbType.Int);
        dp.AddParam("@RetryCount", dto.RetryCount, SqlDbType.Int);
        dp.AddParam("@LastRetryDate", dto.LastRetryDate, SqlDbType.DateTime);
        dp.AddParam("@ProcessedDate", dto.ProcessedDate, SqlDbType.DateTime);
        dp.AddParam("@LastError", dto.LastError, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        return dp;
    }
}
