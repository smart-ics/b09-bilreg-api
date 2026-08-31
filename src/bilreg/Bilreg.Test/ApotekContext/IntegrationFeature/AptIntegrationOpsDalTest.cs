using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Application.ApotekContext.IntegrationFeature.UseCases;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Infrastructure.ApotekContext.IntegrationFeature;
using Bilreg.Infrastructure.ApotekContext.WorklistFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ApotekContext.IntegrationFeature;

public class AptIntegrationOpsDalTest
{
    private static readonly object SchemaLock = new();

    private readonly AptIntegrationOpsDal _opsDal;
    private readonly AptIntegrationTaskDal _taskDal;

    public AptIntegrationOpsDalTest()
    {
        EnsureSchema();
        var opt = ConnStringHelper.GetTestEnv();
        _opsDal = new AptIntegrationOpsDal(opt);
        _taskDal = new AptIntegrationTaskDal(opt);
    }

    [Fact]
    public void List_filters_by_task_type_source_status_and_last_error_without_payload()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var stockFailedId = $"AT{run}SF";
        var billingFailedId = $"AT{run}BF";
        var succeededId = $"AT{run}OK";

        using (var scope = TransHelper.NewScope())
        {
            var stockFailed = NewTask(
                stockFailedId,
                AptIntegrationTaskTypeEnum.StockReserve,
                AptIntegrationSourceKindEnum.Dispensing,
                $"ADP{run}S",
                $"K:{run}:S",
                AptIntegrationDestinationEnum.StockLedger,
                AptIntegrationTaskStatusEnum.Pending);
            _taskDal.Insert(stockFailed);
            stockFailed.ClaimPending();
            stockFailed.MarkFailed("stock neighbor timeout");
            _taskDal.Update(stockFailed);

            var billingFailed = NewTask(
                billingFailedId,
                AptIntegrationTaskTypeEnum.BillingCharge,
                AptIntegrationSourceKindEnum.Invoice,
                $"ASI{run}B",
                $"K:{run}:B",
                AptIntegrationDestinationEnum.TataRekening,
                AptIntegrationTaskStatusEnum.Pending);
            _taskDal.Insert(billingFailed);
            billingFailed.ClaimPending();
            billingFailed.MarkFailed("tarif tidak aktif");
            _taskDal.Update(billingFailed);

            var succeeded = NewTask(
                succeededId,
                AptIntegrationTaskTypeEnum.BillingCharge,
                AptIntegrationSourceKindEnum.Invoice,
                $"ASI{run}O",
                $"K:{run}:O",
                AptIntegrationDestinationEnum.TataRekening,
                AptIntegrationTaskStatusEnum.Pending);
            _taskDal.Insert(succeeded);
            succeeded.ClaimPending();
            succeeded.MarkSucceeded("TR-CHG-1");
            _taskDal.Update(succeeded);
            scope.Complete();
        }

        var byType = _opsDal.List(new AptIntegrationFailureQuery(
            AptIntegrationTaskTypeEnum.StockReserve, null, null, null));
        byType.Select(x => x.IntegrationTaskId).Should().Contain(stockFailedId);
        byType.Should().NotContain(x => x.IntegrationTaskId == billingFailedId);

        var bySource = _opsDal.List(new AptIntegrationFailureQuery(
            null, AptIntegrationSourceKindEnum.Invoice, AptIntegrationTaskStatusEnum.Failed, "tarif"));
        bySource.Should().ContainSingle(x => x.IntegrationTaskId == billingFailedId);
        bySource.Single(x => x.IntegrationTaskId == billingFailedId).SourceKind
            .Should().Be(AptIntegrationSourceKindEnum.Invoice);
        bySource.Single(x => x.IntegrationTaskId == billingFailedId).SourceId
            .Should().Be($"ASI{run}B");

        var byError = _opsDal.List(new AptIntegrationFailureQuery(
            null, null, AptIntegrationTaskStatusEnum.Failed, "timeout"));
        byError.Select(x => x.IntegrationTaskId).Should().Contain(stockFailedId);
        byError.Should().NotContain(x => x.IntegrationTaskId == billingFailedId);

        var succeededRows = _opsDal.List(new AptIntegrationFailureQuery(
            AptIntegrationTaskTypeEnum.BillingCharge,
            AptIntegrationSourceKindEnum.Invoice,
            AptIntegrationTaskStatusEnum.Succeeded,
            null));
        succeededRows.Should().ContainSingle(x => x.IntegrationTaskId == succeededId);
        succeededRows.Single(x => x.IntegrationTaskId == succeededId).CorrelationId.Should().Be("TR-CHG-1");

        Cleanup(stockFailedId, billingFailedId, succeededId);
    }

    private static AptIntegrationTaskModel NewTask(
        string taskId,
        AptIntegrationTaskTypeEnum taskType,
        AptIntegrationSourceKindEnum sourceKind,
        string sourceId,
        string idempotencyKey,
        AptIntegrationDestinationEnum destination,
        AptIntegrationTaskStatusEnum status)
        => AptIntegrationTaskModel.Rehydrate(
            taskId,
            taskType,
            sourceKind,
            sourceId,
            idempotencyKey,
            destination,
            "{\"sensitive\":\"payload\"}",
            status,
            0,
            "",
            new DateTime(3000, 1, 1),
            new DateTime(3000, 1, 1),
            "",
            DateTime.Now);

    private static void Cleanup(params string[] taskIds)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        foreach (var id in taskIds)
            conn.Execute("DELETE FROM BILRG_AptIntegrationTask WHERE IntegrationTaskId=@id", new { id });
    }

    private static void EnsureSchema()
    {
        lock (SchemaLock)
        {
            var opt = ConnStringHelper.GetTestEnv();
            using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
            conn.Open();
            var sqlRoot = FindSqlRoot();
            EnsureTable(conn, sqlRoot, "BILRG_AptIntegrationTask", "BILRG_AptIntegrationTask.sql");
        }
    }

    private static void EnsureTable(SqlConnection conn, string sqlRoot, string tableName, string scriptFile)
    {
        if (conn.ExecuteScalar<int>("SELECT COUNT(1) FROM sys.tables WHERE name = @tableName", new { tableName }) != 0)
            return;
        ExecuteScript(conn, Path.Combine(sqlRoot, scriptFile));
    }

    private static void ExecuteScript(SqlConnection conn, string scriptPath)
    {
        var script = File.ReadAllText(scriptPath);
        var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var batch in batches.Where(x => !string.IsNullOrWhiteSpace(x)))
            conn.Execute(batch);
    }

    private static string FindSqlRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Bilreg.SqlDb", "ApotekContext");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Bilreg.SqlDb/ApotekContext not found");
    }
}
