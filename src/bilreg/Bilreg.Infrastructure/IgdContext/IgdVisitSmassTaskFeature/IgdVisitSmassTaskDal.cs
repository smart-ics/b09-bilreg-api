using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.IgdContext.IgdVisitSmassTaskFeature;

public interface IIgdVisitSmassTaskDal :
    IInsert<IgdVisitSmassTaskDto>,
    IUpdate<IgdVisitSmassTaskDto>,
    IGetData<IgdVisitSmassTaskDto, IIgdVisitSmassTaskKey>
{
    IgdVisitSmassTaskDto? FindByBusinessKey(
        string igdVisitId,
        int noTriage,
        SmassTaskTypeEnum taskType);

    IEnumerable<IgdVisitSmassTaskDto> ListByVisit(string igdVisitId);
    IEnumerable<IgdVisitSmassTaskDto> ListProcessable();
}

public class IgdVisitSmassTaskDal : IIgdVisitSmassTaskDal
{
    private readonly DatabaseOptions _opt;

    public IgdVisitSmassTaskDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IgdVisitSmassTaskDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_IgdVisitSmassTask (
                IgdVisitSmassTaskId, IgdVisitId, NoTriage, TaskType, TaskStatus,
                AssessmentId, RetryCount, LastRetryDate, ProcessedDate, LastError, CrtDate)
            VALUES (
                @IgdVisitSmassTaskId, @IgdVisitId, @NoTriage, @TaskType, @TaskStatus,
                @AssessmentId, @RetryCount, @LastRetryDate, @ProcessedDate, @LastError, @CrtDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(IgdVisitSmassTaskDto dto)
    {
        // CrtDate and the business-key columns are immutable after creation.
        const string sql = """
            UPDATE BILRG_IgdVisitSmassTask SET
                TaskStatus = @TaskStatus,
                AssessmentId = @AssessmentId,
                RetryCount = @RetryCount,
                LastRetryDate = @LastRetryDate,
                ProcessedDate = @ProcessedDate,
                LastError = @LastError
            WHERE IgdVisitSmassTaskId = @IgdVisitSmassTaskId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public IgdVisitSmassTaskDto GetData(IIgdVisitSmassTaskKey key)
    {
        const string sql = """
            SELECT
                aa.IgdVisitSmassTaskId, aa.IgdVisitId, aa.NoTriage, aa.TaskType, aa.TaskStatus,
                aa.AssessmentId, aa.RetryCount, aa.LastRetryDate, aa.ProcessedDate, aa.LastError, aa.CrtDate
            FROM BILRG_IgdVisitSmassTask aa
            WHERE aa.IgdVisitSmassTaskId = @IgdVisitSmassTaskId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitSmassTaskId", key.IgdVisitSmassTaskId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        // IGetData<,> declares a non-null return; the repo treats a null result as MayBe.None.
        return conn.QueryFirstOrDefault<IgdVisitSmassTaskDto>(sql, dp)!;
    }

    public IgdVisitSmassTaskDto? FindByBusinessKey(
        string igdVisitId,
        int noTriage,
        SmassTaskTypeEnum taskType)
    {
        const string sql = """
            SELECT
                aa.IgdVisitSmassTaskId, aa.IgdVisitId, aa.NoTriage, aa.TaskType, aa.TaskStatus,
                aa.AssessmentId, aa.RetryCount, aa.LastRetryDate, aa.ProcessedDate, aa.LastError, aa.CrtDate
            FROM BILRG_IgdVisitSmassTask aa
            WHERE aa.IgdVisitId = @IgdVisitId
              AND aa.NoTriage = @NoTriage
              AND aa.TaskType = @TaskType
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", igdVisitId, SqlDbType.VarChar);
        dp.AddParam("@NoTriage", noTriage, SqlDbType.Int);
        dp.AddParam("@TaskType", (int)taskType, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<IgdVisitSmassTaskDto>(sql, dp);
    }

    public IEnumerable<IgdVisitSmassTaskDto> ListByVisit(string igdVisitId)
    {
        const string sql = """
            SELECT
                aa.IgdVisitSmassTaskId, aa.IgdVisitId, aa.NoTriage, aa.TaskType, aa.TaskStatus,
                aa.AssessmentId, aa.RetryCount, aa.LastRetryDate, aa.ProcessedDate, aa.LastError, aa.CrtDate
            FROM BILRG_IgdVisitSmassTask aa
            WHERE aa.IgdVisitId = @IgdVisitId
            ORDER BY aa.CrtDate ASC, aa.NoTriage ASC, aa.TaskType ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", igdVisitId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<IgdVisitSmassTaskDto>(sql, dp).ToList();
    }

    public IEnumerable<IgdVisitSmassTaskDto> ListProcessable()
    {
        const string sql = """
            SELECT
                aa.IgdVisitSmassTaskId, aa.IgdVisitId, aa.NoTriage, aa.TaskType, aa.TaskStatus,
                aa.AssessmentId, aa.RetryCount, aa.LastRetryDate, aa.ProcessedDate, aa.LastError, aa.CrtDate
            FROM BILRG_IgdVisitSmassTask aa
            WHERE aa.TaskStatus = @Failed
            ORDER BY aa.CrtDate ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@Failed", (int)SmassTaskStatusEnum.Failed, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<IgdVisitSmassTaskDto>(sql, dp).ToList();
    }

    private static DynamicParameters BuildParams(IgdVisitSmassTaskDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitSmassTaskId", dto.IgdVisitSmassTaskId, SqlDbType.VarChar);
        dp.AddParam("@IgdVisitId", dto.IgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@NoTriage", dto.NoTriage, SqlDbType.Int);
        dp.AddParam("@TaskType", dto.TaskType, SqlDbType.Int);
        dp.AddParam("@TaskStatus", dto.TaskStatus, SqlDbType.Int);
        dp.AddParam("@AssessmentId", dto.AssessmentId, SqlDbType.VarChar);
        dp.AddParam("@RetryCount", dto.RetryCount, SqlDbType.Int);
        dp.AddParam("@LastRetryDate", dto.LastRetryDate, SqlDbType.DateTime);
        dp.AddParam("@ProcessedDate", dto.ProcessedDate, SqlDbType.DateTime);
        dp.AddParam("@LastError", dto.LastError, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        return dp;
    }
}
