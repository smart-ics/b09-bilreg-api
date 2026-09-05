using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Infrastructure.ApotekContext.IntegrationFeature;
using Bilreg.Infrastructure.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.ApotekContext.SalesOrderFeature;
using Bilreg.Infrastructure.ApotekContext.TelaahResepFeature;
using Bilreg.Infrastructure.ApotekContext.WorklistFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.WorklistFeature;

public class AptWorklistJourneyDalTest
{
    private static readonly object SchemaLock = new();

    private readonly AptWorklistDal _worklist;
    private readonly QueueMappingRepo _mapRepo;
    private readonly SalesOrderRepo _orderRepo;
    private readonly TelaahResepRepo _telaahRepo;
    private readonly AptIntegrationTaskDal _taskDal;

    public AptWorklistJourneyDalTest()
    {
        EnsureSchema();
        var opt = ConnStringHelper.GetTestEnv();
        _worklist = new(opt);
        _mapRepo = new QueueMappingRepo(new QueueMappingDal(opt));
        _orderRepo = new(
            new SalesOrderDal(opt),
            new SalesOrderItemDal(opt),
            new SalesOrderItemComponentDal(opt),
            new UnfulfilledOutcomeDal(opt));
        _telaahRepo = new TelaahResepRepo(new TelaahDal(opt), new TelaahItemDal(opt));
        _taskDal = new AptIntegrationTaskDal(opt);
    }

    [Fact]
    public void LoadJourney_preserves_mixed_payer_paths_and_open_integration_tasks_per_demand()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var antrianId = $"ANT-J-{run}";
        const int noUrut = 1;
        var resepId = $"ARX{run}";
        var jualBebasId = $"ADQ{run}";
        var telaahId = $"ATR{run}";
        var regId = "R" + run[..3];
        var orderGeneralId = $"ASO{run}G";
        var orderBpjsId = $"ASO{run}B";
        var orderJbId = $"ASO{run}J";
        var pendingTaskId = $"AIT{run}P";
        var failedTaskId = $"AIT{run}F";
        var succeededTaskId = $"AIT{run}S";

        Cleanup(
            antrianId, resepId, jualBebasId, telaahId,
            orderGeneralId, orderBpjsId, orderJbId,
            pendingTaskId, failedTaskId, succeededTaskId);

        _mapRepo.SaveChanges(QueueMappingModel.Create(
            QueueDemandKindEnum.ResepKerja, resepId, antrianId, noUrut, "TRK1",
            QueueMappingMethodEnum.Manual, "mapper", DateTime.Now));
        _mapRepo.SaveChanges(QueueMappingModel.Create(
            QueueDemandKindEnum.JualBebas, jualBebasId, antrianId, noUrut, "TRK1",
            QueueMappingMethodEnum.Manual, "mapper", DateTime.Now));

        _telaahRepo.SaveChanges(TelaahResepModel.Rehydrate(
            telaahId, resepId, regId, TelaahStatusEnum.Approved, "ph1",
            DateTime.Now, DateTime.Now, 1,
            [new TelaahResepItemModel(1, 1, TelaahDispositionEnum.AcceptedAsPrescribed, "BRG1", "Obat", 10m, "", "ph1")]));

        _orderRepo.SaveChanges(SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.ResepKerja, resepId, telaahId, regId, "P1", "Pasien A",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, $"BRG{run}G", "Obat", "TAB", 5, FornasCoverageEnum.Unknown, "", false)],
            []));
        _orderRepo.SaveChanges(SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.ResepKerja, resepId, telaahId, regId, "P1", "Pasien A",
            PayerPathEnum.Bpjs, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, $"BRG{run}B", "Obat", "TAB", 5, FornasCoverageEnum.Covered, "", false)],
            []));
        _orderRepo.SaveChanges(SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, jualBebasId, "", regId, "P1", "Pasien B",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, $"BRG{run}J", "Obat", "TAB", 2, FornasCoverageEnum.Unknown, "", false)],
            []));

        var generalOrder = _orderRepo.LoadActive(
            SalesOrderSourceKindEnum.ResepKerja, resepId, regId, PayerPathEnum.GeneralPatientPay).Value;
        var bpjsOrder = _orderRepo.LoadActive(
            SalesOrderSourceKindEnum.ResepKerja, resepId, regId, PayerPathEnum.Bpjs).Value;
        var jbOrder = _orderRepo.LoadActive(
            SalesOrderSourceKindEnum.JualBebas, jualBebasId, regId, PayerPathEnum.GeneralPatientPay).Value;

        _taskDal.Insert(NewTask(pendingTaskId, generalOrder.SalesOrderId, AptIntegrationTaskStatusEnum.Pending));
        var failed = NewTask(failedTaskId, bpjsOrder.SalesOrderId, AptIntegrationTaskStatusEnum.Pending);
        _taskDal.Insert(failed);
        failed.ClaimPending();
        failed.MarkFailed("neighbor unavailable");
        _taskDal.Update(failed);
        _taskDal.Insert(NewTask(succeededTaskId, jbOrder.SalesOrderId, AptIntegrationTaskStatusEnum.Succeeded));

        var journey = _worklist.LoadJourney(antrianId, noUrut);

        journey.AntrianId.Should().Be(antrianId);
        journey.NoUrut.Should().Be(noUrut);
        journey.Demands.Should().HaveCount(2);

        var resepDemand = journey.Demands.Single(x => x.DemandId == resepId);
        resepDemand.TelaahResepId.Should().Be(telaahId);
        resepDemand.TelaahStatus.Should().Be(TelaahStatusEnum.Approved);
        resepDemand.SalesOrders.Select(x => x.PayerPath).Should().Equal(
            PayerPathEnum.GeneralPatientPay, PayerPathEnum.Bpjs);
        resepDemand.IntegrationTasks.Should().HaveCount(2);
        resepDemand.IntegrationTasks.Should().Contain(x =>
            x.IntegrationTaskId == pendingTaskId && x.Status == AptIntegrationTaskStatusEnum.Pending);
        resepDemand.IntegrationTasks.Should().Contain(x =>
            x.IntegrationTaskId == failedTaskId && x.Status == AptIntegrationTaskStatusEnum.Failed && x.LastError == "neighbor unavailable");

        var jbDemand = journey.Demands.Single(x => x.DemandId == jualBebasId);
        jbDemand.TelaahResepId.Should().BeEmpty();
        jbDemand.TelaahStatus.Should().BeNull();
        jbDemand.SalesOrders.Should().ContainSingle(x => x.PayerPath == PayerPathEnum.GeneralPatientPay);
        jbDemand.IntegrationTasks.Should().BeEmpty();

        Cleanup(
            antrianId, resepId, jualBebasId, telaahId,
            generalOrder.SalesOrderId, bpjsOrder.SalesOrderId, jbOrder.SalesOrderId,
            pendingTaskId, failedTaskId, succeededTaskId);
    }

    private static AptIntegrationTaskModel NewTask(
        string taskId,
        string sourceId,
        AptIntegrationTaskStatusEnum status)
        => AptIntegrationTaskModel.Rehydrate(
            taskId,
            AptIntegrationTaskTypeEnum.BillingCharge,
            AptIntegrationSourceKindEnum.SalesOrder,
            sourceId,
            $"K:{taskId}",
            AptIntegrationDestinationEnum.TataRekening,
            "{}",
            status,
            0,
            "",
            new DateTime(3000, 1, 1),
            new DateTime(3000, 1, 1),
            "",
            DateTime.Now);

    private static void Cleanup(
        string antrianId,
        string resepId,
        string jualBebasId,
        string telaahId,
        params string[] salesOrderAndTaskIds)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM BILRG_AptQueueMapping WHERE DemandId IN (@resepId, @jualBebasId)",
            new { resepId, jualBebasId });
        conn.Execute("DELETE FROM BILRG_AptTelaahResepItem WHERE TelaahResepId=@telaahId", new { telaahId });
        conn.Execute("DELETE FROM BILRG_AptTelaahResep WHERE TelaahResepId=@telaahId", new { telaahId });
        foreach (var id in salesOrderAndTaskIds)
        {
            conn.Execute("DELETE FROM BILRG_AptUnfulfilledOutcome WHERE SalesOrderId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptSalesOrderItem WHERE SalesOrderId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptSalesOrderItemComponent WHERE SalesOrderId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptSalesOrder WHERE SalesOrderId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptIntegrationTask WHERE IntegrationTaskId=@id", new { id });
        }
    }

    private static void EnsureSchema()
    {
        lock (SchemaLock)
        {
            var opt = ConnStringHelper.GetTestEnv();
            using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
            conn.Open();
            var sqlRoot = FindSqlRoot();
            EnsureTable(conn, sqlRoot, "BILRG_AptQueueMapping", "BILRG_AptQueueMapping.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptTelaahResep", "BILRG_AptTelaahResep.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptTelaahResepItem", "BILRG_AptTelaahResepItem.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptSalesOrder", "BILRG_AptSalesOrder.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptSalesOrderItem", "BILRG_AptSalesOrderItem.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptSalesOrderItemComponent", "BILRG_AptSalesOrderItemComponent.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptUnfulfilledOutcome", "BILRG_AptUnfulfilledOutcome.sql");
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

        throw new InvalidOperationException("Unable to locate Bilreg.SqlDb/ApotekContext.");
    }
}
