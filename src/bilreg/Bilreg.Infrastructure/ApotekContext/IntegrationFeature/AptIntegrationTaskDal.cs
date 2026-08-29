using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ApotekContext.IntegrationFeature;

public class AptIntegrationTaskDal : IAptIntegrationTaskDal
{
    private readonly DatabaseOptions _opt;

    public AptIntegrationTaskDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AptIntegrationTaskModel model)
    {
        const string sql = """
            INSERT INTO BILRG_AptIntegrationTask (
                IntegrationTaskId, TaskType, SourceKind, SourceId, IdempotencyKey,
                Destination, PayloadJson, TaskStatus, RetryCount, LastError,
                LastRetryDate, ProcessedDate, CorrelationId, CrtDate)
            VALUES (
                @IntegrationTaskId, @TaskType, @SourceKind, @SourceId, @IdempotencyKey,
                @Destination, @PayloadJson, @TaskStatus, @RetryCount, @LastError,
                @LastRetryDate, @ProcessedDate, @CorrelationId, @CrtDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(AptIntegrationTaskDto.FromModel(model)));
    }

    public void Update(AptIntegrationTaskModel model)
    {
        const string sql = """
            UPDATE BILRG_AptIntegrationTask SET
                TaskType = @TaskType,
                SourceKind = @SourceKind,
                SourceId = @SourceId,
                IdempotencyKey = @IdempotencyKey,
                Destination = @Destination,
                PayloadJson = @PayloadJson,
                TaskStatus = @TaskStatus,
                RetryCount = @RetryCount,
                LastError = @LastError,
                LastRetryDate = @LastRetryDate,
                ProcessedDate = @ProcessedDate,
                CorrelationId = @CorrelationId
            WHERE IntegrationTaskId = @IntegrationTaskId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(AptIntegrationTaskDto.FromModel(model)));
    }

    public AptIntegrationTaskModel GetData(IAptIntegrationTaskKey key)
    {
        const string sql = """
            SELECT
                aa.IntegrationTaskId, aa.TaskType, aa.SourceKind, aa.SourceId, aa.IdempotencyKey,
                aa.Destination, aa.PayloadJson, aa.TaskStatus, aa.RetryCount, aa.LastError,
                aa.LastRetryDate, aa.ProcessedDate, aa.CorrelationId, aa.CrtDate
            FROM BILRG_AptIntegrationTask aa
            WHERE aa.IntegrationTaskId = @IntegrationTaskId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IntegrationTaskId", key.IntegrationTaskId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dto = conn.QueryFirstOrDefault<AptIntegrationTaskDto>(sql, dp);
        return dto?.ToModel()!;
    }

    public AptIntegrationTaskModel? GetByIdempotencyKey(string idempotencyKey)
    {
        const string sql = """
            SELECT
                aa.IntegrationTaskId, aa.TaskType, aa.SourceKind, aa.SourceId, aa.IdempotencyKey,
                aa.Destination, aa.PayloadJson, aa.TaskStatus, aa.RetryCount, aa.LastError,
                aa.LastRetryDate, aa.ProcessedDate, aa.CorrelationId, aa.CrtDate
            FROM BILRG_AptIntegrationTask aa
            WHERE aa.IdempotencyKey = @IdempotencyKey
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IdempotencyKey", idempotencyKey, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<AptIntegrationTaskDto>(sql, dp)?.ToModel();
    }

    public IEnumerable<AptIntegrationTaskModel> ListPending(int batchSize)
    {
        const string sql = """
            SELECT TOP (@BatchSize)
                aa.IntegrationTaskId, aa.TaskType, aa.SourceKind, aa.SourceId, aa.IdempotencyKey,
                aa.Destination, aa.PayloadJson, aa.TaskStatus, aa.RetryCount, aa.LastError,
                aa.LastRetryDate, aa.ProcessedDate, aa.CorrelationId, aa.CrtDate
            FROM BILRG_AptIntegrationTask aa
            WHERE aa.TaskStatus IN (@Pending, @Failed)
            ORDER BY aa.CrtDate ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BatchSize", batchSize, SqlDbType.Int);
        dp.AddParam("@Pending", (int)AptIntegrationTaskStatusEnum.Pending, SqlDbType.Int);
        dp.AddParam("@Failed", (int)AptIntegrationTaskStatusEnum.Failed, SqlDbType.Int);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<AptIntegrationTaskDto>(sql, dp).Select(x => x.ToModel()).ToList();
    }

    public bool ClaimPending(IAptIntegrationTaskKey key)
    {
        const string sql = """
            UPDATE BILRG_AptIntegrationTask
            SET TaskStatus = @Processing
            WHERE IntegrationTaskId = @IntegrationTaskId
              AND TaskStatus IN (@Pending, @Failed)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IntegrationTaskId", key.IntegrationTaskId, SqlDbType.VarChar);
        dp.AddParam("@Processing", (int)AptIntegrationTaskStatusEnum.Processing, SqlDbType.Int);
        dp.AddParam("@Pending", (int)AptIntegrationTaskStatusEnum.Pending, SqlDbType.Int);
        dp.AddParam("@Failed", (int)AptIntegrationTaskStatusEnum.Failed, SqlDbType.Int);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp) == 1;
    }

    private static DynamicParameters BuildParams(AptIntegrationTaskDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@IntegrationTaskId", dto.IntegrationTaskId, SqlDbType.VarChar);
        dp.AddParam("@TaskType", dto.TaskType, SqlDbType.Int);
        dp.AddParam("@SourceKind", dto.SourceKind, SqlDbType.Int);
        dp.AddParam("@SourceId", dto.SourceId, SqlDbType.VarChar);
        dp.AddParam("@IdempotencyKey", dto.IdempotencyKey, SqlDbType.VarChar);
        dp.AddParam("@Destination", dto.Destination, SqlDbType.Int);
        dp.AddParam("@PayloadJson", dto.PayloadJson, SqlDbType.NVarChar);
        dp.AddParam("@TaskStatus", dto.TaskStatus, SqlDbType.Int);
        dp.AddParam("@RetryCount", dto.RetryCount, SqlDbType.Int);
        dp.AddParam("@LastError", dto.LastError, SqlDbType.VarChar);
        dp.AddParam("@LastRetryDate", dto.LastRetryDate, SqlDbType.DateTime);
        dp.AddParam("@ProcessedDate", dto.ProcessedDate, SqlDbType.DateTime);
        dp.AddParam("@CorrelationId", dto.CorrelationId, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        return dp;
    }
}
