using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.EmrAntrianOutboundFeature;

public interface IEmrAntrianOutboundQueueDal :
    IInsert<EmrAntrianOutboundQueueDto>,
    IUpdate<EmrAntrianOutboundQueueDto>,
    IGetData<EmrAntrianOutboundQueueDto, IEmrAntrianOutboundQueueKey>,
    IDelete<IEmrAntrianOutboundQueueKey>
{
    IEnumerable<EmrAntrianOutboundQueueDto> ListProcessable(int batchSize);

    EmrAntrianOutboundQueueDto? FindActiveBySource(string sourceId, string messageType);
    void DeleteBySourceId(string sourceId);
}

public class EmrAntrianOutboundQueueDal : IEmrAntrianOutboundQueueDal
{
    private readonly DatabaseOptions _opt;

    public EmrAntrianOutboundQueueDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(EmrAntrianOutboundQueueDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_EmrAntrianOutboundQueue (
                QueueId, SourceId, MessageType, PayloadJson, QueueStatus,
                RetryCount, LastRetryDate, ProcessedDate, LastError, CrtDate)
            VALUES (
                @QueueId, @SourceId, @MessageType, @PayloadJson, @QueueStatus,
                @RetryCount, @LastRetryDate, @ProcessedDate, @LastError, @CrtDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(EmrAntrianOutboundQueueDto dto)
    {
        const string sql = """
            UPDATE BILRG_EmrAntrianOutboundQueue SET
                SourceId = @SourceId,
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

    public EmrAntrianOutboundQueueDto GetData(IEmrAntrianOutboundQueueKey key)
    {
        const string sql = """
            SELECT
                aa.QueueId, aa.SourceId, aa.MessageType, aa.PayloadJson, aa.QueueStatus,
                aa.RetryCount, aa.LastRetryDate, aa.ProcessedDate, aa.LastError, aa.CrtDate
            FROM BILRG_EmrAntrianOutboundQueue aa
            WHERE aa.QueueId = @QueueId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@QueueId", key.QueueId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<EmrAntrianOutboundQueueDto>(sql, dp);
    }

    public IEnumerable<EmrAntrianOutboundQueueDto> ListProcessable(int batchSize)
    {
        const string sql = """
            SELECT TOP (@BatchSize)
                aa.QueueId, aa.SourceId, aa.MessageType, aa.PayloadJson, aa.QueueStatus,
                aa.RetryCount, aa.LastRetryDate, aa.ProcessedDate, aa.LastError, aa.CrtDate
            FROM BILRG_EmrAntrianOutboundQueue aa
            WHERE aa.QueueStatus IN (@Pending, @Failed)
            ORDER BY aa.CrtDate ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BatchSize", batchSize, SqlDbType.Int);
        dp.AddParam("@Pending", (int)EmrAntrianOutboundQueueStatusEnum.Pending, SqlDbType.Int);
        dp.AddParam("@Failed", (int)EmrAntrianOutboundQueueStatusEnum.Failed, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<EmrAntrianOutboundQueueDto>(sql, dp).ToList();
    }

    public EmrAntrianOutboundQueueDto? FindActiveBySource(string sourceId, string messageType)
    {
        const string sql = """
            SELECT TOP 1
                aa.QueueId, aa.SourceId, aa.MessageType, aa.PayloadJson, aa.QueueStatus,
                aa.RetryCount, aa.LastRetryDate, aa.ProcessedDate, aa.LastError, aa.CrtDate
            FROM BILRG_EmrAntrianOutboundQueue aa
            WHERE aa.SourceId = @SourceId
              AND aa.MessageType = @MessageType
              AND aa.QueueStatus IN (@Pending, @Processing, @Succeeded)
            ORDER BY aa.CrtDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@SourceId", sourceId, SqlDbType.VarChar);
        dp.AddParam("@MessageType", messageType, SqlDbType.VarChar);
        dp.AddParam("@Pending", (int)EmrAntrianOutboundQueueStatusEnum.Pending, SqlDbType.Int);
        dp.AddParam("@Processing", (int)EmrAntrianOutboundQueueStatusEnum.Processing, SqlDbType.Int);
        dp.AddParam("@Succeeded", (int)EmrAntrianOutboundQueueStatusEnum.Succeeded, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<EmrAntrianOutboundQueueDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(EmrAntrianOutboundQueueDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@QueueId", dto.QueueId, SqlDbType.VarChar);
        dp.AddParam("@SourceId", dto.SourceId, SqlDbType.VarChar);
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

    public void Delete(IEmrAntrianOutboundQueueKey key)
    {
        const string sql = """
           DELETE BILRG_EmrAntrianOutboundQueue 
           WHERE QueueId = @QueueId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@QueueId", key.QueueId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void DeleteBySourceId(string sourceId)
    {
        const string sql = """
           DELETE BILRG_EmrAntrianOutboundQueue 
           WHERE SourceId = @SourceId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@SourceId", sourceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
