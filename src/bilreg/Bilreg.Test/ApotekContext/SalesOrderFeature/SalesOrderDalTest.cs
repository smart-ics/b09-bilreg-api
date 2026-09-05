using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Infrastructure.ApotekContext.SalesOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.SalesOrderFeature;

public class SalesOrderDalTest
{
    private static readonly object SchemaLock = new();

    private readonly SalesOrderDal _dal;
    private readonly SalesOrderItemDal _itemDal;
    private readonly SalesOrderItemComponentDal _componentDal;
    private readonly UnfulfilledOutcomeDal _outcomeDal;
    private readonly SalesOrderRepo _repo;

    public SalesOrderDalTest()
    {
        EnsureSchema();
        var opt = ConnStringHelper.GetTestEnv();
        _dal = new(opt);
        _itemDal = new(opt);
        _componentDal = new(opt);
        _outcomeDal = new(opt);
        _repo = new(_dal, _itemDal, _componentDal, _outcomeDal);
    }

    [Fact]
    public void RoundTrip_preserves_header_items_and_quantities()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.ResepKerja, $"ARX{run}", $"ATR{run}", "R" + run[..3], "P" + run[..3], "Pasien Dal",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, $"BRG{run}", "Obat", "TAB", 10, FornasCoverageEnum.Unknown, "", false)],
            []);
        Cleanup(order.SalesOrderId);

        _repo.SaveChanges(order);

        var loaded = _repo.LoadEntity(order).Value;
        loaded.SourceKind.Should().Be(SalesOrderSourceKindEnum.ResepKerja);
        loaded.SourceId.Should().Be($"ARX{run}");
        loaded.TelaahResepId.Should().Be($"ATR{run}");
        loaded.RegId.Should().Be("R" + run[..3]);
        loaded.PayerPath.Should().Be(PayerPathEnum.GeneralPatientPay);
        loaded.SalesOrderStatus.Should().Be(SalesOrderStatusEnum.Established);
        loaded.Items.Should().ContainSingle();
        loaded.Items[0].BrgId.Should().Be($"BRG{run}");
        loaded.Items[0].AcceptedQty.Should().Be(10m);
        loaded.Items[0].InvoicedQty.Should().Be(0m);

        Cleanup(order.SalesOrderId);
    }

    [Fact]
    public void Filtered_unique_index_rejects_duplicate_active_source_reg_payer()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var idFirst = $"ASO{run}A";
        var idDup = $"ASO{run}B";
        var idOtherPayer = $"ASO{run}C";
        var idResolved = $"ASO{run}D";
        var sourceId = $"ARX{run}";
        var regId = "R" + run[..3];
        const int sourceKind = (int)SalesOrderSourceKindEnum.ResepKerja;
        const int payerGeneral = (int)PayerPathEnum.GeneralPatientPay;
        const int payerBpjs = (int)PayerPathEnum.Bpjs;
        Cleanup(idFirst, idDup, idOtherPayer, idResolved);

        InsertHeader(idFirst, sourceKind, sourceId, regId, payerGeneral, (int)SalesOrderStatusEnum.Established);

        const string duplicateSql = """
            INSERT INTO BILRG_AptSalesOrder (SalesOrderId, SourceKind, SourceId, RegId, PayerPath, SalesOrderStatus)
            VALUES (@id, @sourceKind, @sourceId, @regId, @payerPath, @status)
            """;
        var opt = ConnStringHelper.GetTestEnv();
        using (var conn = new SqlConnection(ConnStringHelper.Get(opt.Value)))
        {
            var act = () => conn.Execute(duplicateSql, new
            {
                id = idDup,
                sourceKind,
                sourceId,
                regId,
                payerPath = payerGeneral,
                status = (int)SalesOrderStatusEnum.Active
            });
            act.Should().Throw<SqlException>().Where(ex => ex.Number == 2601 || ex.Number == 2627);
        }

        using (var conn = new SqlConnection(ConnStringHelper.Get(opt.Value)))
            conn.Execute(duplicateSql, new
            {
                id = idOtherPayer,
                sourceKind,
                sourceId,
                regId,
                payerPath = payerBpjs,
                status = (int)SalesOrderStatusEnum.Established
            });

        using (var conn = new SqlConnection(ConnStringHelper.Get(opt.Value)))
            conn.Execute(duplicateSql, new
            {
                id = idResolved,
                sourceKind,
                sourceId,
                regId,
                payerPath = payerGeneral,
                status = (int)SalesOrderStatusEnum.Resolved
            });

        Cleanup(idFirst, idDup, idOtherPayer, idResolved);
    }

    [Fact]
    public void GetActive_returns_established_or_active_only()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, $"ADQ{run}", "", "R" + run[..3], "P" + run[..3], "Pasien",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, $"BRG{run}", "Obat", "TAB", 2, FornasCoverageEnum.Unknown, "", false)],
            []);
        Cleanup(order.SalesOrderId);
        _repo.SaveChanges(order);

        _dal.GetActive(
                (int)order.SourceKind, order.SourceId, order.RegId, (int)order.PayerPath)
            .Should().NotBeNull();
        _repo.LoadActive(order.SourceKind, order.SourceId, order.RegId, order.PayerPath).HasValue
            .Should().BeTrue();

        order.Resolve(SalesOrderResolvedReasonEnum.FullyFulfilled);
        _repo.SaveChanges(order);

        _dal.GetActive(
                (int)order.SourceKind, order.SourceId, order.RegId, (int)order.PayerPath)
            .Should().BeNull();
        _repo.LoadActive(order.SourceKind, order.SourceId, order.RegId, order.PayerPath).HasValue
            .Should().BeFalse();

        Cleanup(order.SalesOrderId);
    }

    [Fact]
    public void Append_only_outcomes_persist_without_rewrite_or_delete()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.ResepKerja, $"ARX{run}", $"ATR{run}", "R" + run[..3], "P" + run[..3], "Pasien Dal",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, $"BRG{run}", "Obat", "TAB", 10, FornasCoverageEnum.Unknown, "", false)],
            []);
        Cleanup(order.SalesOrderId);

        _repo.SaveChanges(order);

        order.AppendUnfulfilled(1, 2, UnfulfilledReasonEnum.StockShortageAfterEstablishment, "ACR1", "actor1", DateTime.Now);
        _repo.SaveChanges(order);

        order.AppendUnfulfilled(1, 1, UnfulfilledReasonEnum.PatientDecline, "ACR2", "actor2", DateTime.Now);
        _repo.SaveChanges(order);

        var loaded = _repo.LoadEntity(order).Value;
        loaded.Outcomes.Should().HaveCount(2);
        loaded.Outcomes.Select(x => x.OutcomeNo).Should().BeEquivalentTo([1, 2]);
        loaded.Outcomes.Should().Contain(x => x.Qty == 2m && x.Reason == UnfulfilledReasonEnum.StockShortageAfterEstablishment);
        loaded.Outcomes.Should().Contain(x => x.Qty == 1m && x.Reason == UnfulfilledReasonEnum.PatientDecline);

        var opt = ConnStringHelper.GetTestEnv();
        using (var conn = new SqlConnection(ConnStringHelper.Get(opt.Value)))
        {
            var count = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM BILRG_AptUnfulfilledOutcome WHERE SalesOrderId=@id",
                new { id = order.SalesOrderId });
            count.Should().Be(2);
        }

        Cleanup(order.SalesOrderId);
    }

    private static void EnsureSchema()
    {
        lock (SchemaLock)
        {
            var opt = ConnStringHelper.GetTestEnv();
            using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
            conn.Open();

            var sqlRoot = FindSqlRoot();
            EnsureTable(conn, sqlRoot, "BILRG_AptSalesOrder", "BILRG_AptSalesOrder.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptSalesOrderItem", "BILRG_AptSalesOrderItem.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptSalesOrderItemComponent", "BILRG_AptSalesOrderItemComponent.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptUnfulfilledOutcome", "BILRG_AptUnfulfilledOutcome.sql");
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

    private void InsertHeader(string id, int sourceKind, string sourceId, string regId, int payerPath, int status)
    {
        var opt = ConnStringHelper.GetTestEnv();
        using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
        conn.Execute("""
            INSERT INTO BILRG_AptSalesOrder (SalesOrderId, SourceKind, SourceId, RegId, PayerPath, SalesOrderStatus)
            VALUES (@id, @sourceKind, @sourceId, @regId, @payerPath, @status)
            """, new { id, sourceKind, sourceId, regId, payerPath, status });
    }

    private void Cleanup(params string[] ids)
    {
        var opt = ConnStringHelper.GetTestEnv();
        using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
        foreach (var id in ids)
        {
            conn.Execute("DELETE FROM BILRG_AptUnfulfilledOutcome WHERE SalesOrderId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptSalesOrderItem WHERE SalesOrderId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptSalesOrderItemComponent WHERE SalesOrderId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptSalesOrder WHERE SalesOrderId=@id", new { id });
        }
    }
}
