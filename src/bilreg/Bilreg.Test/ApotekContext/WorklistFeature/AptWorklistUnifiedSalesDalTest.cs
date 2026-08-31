using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Infrastructure.ApotekContext.InvoiceFeature;
using Bilreg.Infrastructure.ApotekContext.SalesOrderFeature;
using Bilreg.Infrastructure.ApotekContext.WorklistFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.WorklistFeature;

public class AptWorklistUnifiedSalesDalTest
{
    private static readonly object SchemaLock = new();

    private readonly AptWorklistDal _sut;
    private readonly SalesOrderRepo _orderRepo;
    private readonly InvoiceRepo _invoiceRepo;

    public AptWorklistUnifiedSalesDalTest()
    {
        EnsureSchema();
        var opt = ConnStringHelper.GetTestEnv();
        _sut = new(opt);
        _orderRepo = new(
            new SalesOrderDal(opt),
            new SalesOrderItemDal(opt),
            new SalesOrderItemComponentDal(opt),
            new UnfulfilledOutcomeDal(opt));
        _invoiceRepo = new(new InvoiceDal(opt), new InvoiceItemDal(opt), new InvoiceChargeDal(opt));
    }

    [Fact]
    public void ListUnifiedSales_unions_apt_and_du_rows_with_source_discriminator_and_shared_document_id()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var establishedAt = new DateTime(2026, 8, 27, 10, 30, 0);
        var date1 = new DateTime(2026, 8, 27);
        var date2 = new DateTime(2026, 8, 28);

        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, $"ADQ{run}", "", "R" + run[..3], "P" + run[..3], "Pasien Apt",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, $"BRG{run}", "Obat", "TAB", 1, FornasCoverageEnum.Unknown, "", false)],
            []);
        _orderRepo.SaveChanges(order);

        var invoice = InvoiceModel.Establish(
            order.SalesOrderId,
            PayerPathEnum.GeneralPatientPay,
            establishedAt,
            "UMUM",
            "Umum",
            establishedAt,
            [new InvoiceItemModel(1, 1, $"BRG{run}", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [],
            0, 0, 0);
        _invoiceRepo.SaveChanges(invoice);
        var aptDocId = invoice.InvoiceId;
        var sharedDocId = $"D{run[..8]}";
        var voidDocId = $"V{run[..8]}";

        InsertDuRow(sharedDocId, "20260827", "Pasien Du", 250m, voided: false);
        InsertDuRow(voidDocId, "20260827", "Void Du", 999m, voided: true);

        var results = _sut.ListUnifiedSales(date1, date2);

        results.Should().Contain(x =>
            x.SourceKind == "APT"
            && x.DocumentId == aptDocId
            && x.DocumentDate == establishedAt
            && x.PasienName == "Pasien Apt"
            && x.GrandTotal == 100m);
        results.Should().Contain(x =>
            x.SourceKind == "DU"
            && x.DocumentId == sharedDocId
            && x.PasienName == "Pasien Du"
            && x.GrandTotal == 250m);
        results.Should().NotContain(x => x.DocumentId == voidDocId);

        CleanupInvoice(aptDocId, order.SalesOrderId);
        CleanupDu(sharedDocId, voidDocId);
    }

    private static void InsertDuRow(string docId, string tglTrs, string pasienName, decimal grandTotal, bool voided)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("""
            INSERT INTO tb_trs_dobill_umum (fs_kd_trs, fd_tgl_trs, fs_nm_pasien, fn_grand_total, fd_tgl_void)
            VALUES (@docId, @tglTrs, @pasienName, @grandTotal, @tglVoid)
            """, new
        {
            docId,
            tglTrs,
            pasienName,
            grandTotal,
            tglVoid = voided ? "20260827" : ""
        });
    }

    private static void CleanupInvoice(string invoiceId, string salesOrderId)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM BILRG_AptInvoiceItem WHERE InvoiceId=@invoiceId", new { invoiceId });
        conn.Execute("DELETE FROM BILRG_AptInvoice WHERE InvoiceId=@invoiceId", new { invoiceId });
        conn.Execute("DELETE FROM BILRG_AptSalesOrderItem WHERE SalesOrderId=@salesOrderId", new { salesOrderId });
        conn.Execute("DELETE FROM BILRG_AptSalesOrder WHERE SalesOrderId=@salesOrderId", new { salesOrderId });
    }

    private static void CleanupDu(params string[] docIds)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        foreach (var docId in docIds)
            conn.Execute("DELETE FROM tb_trs_dobill_umum WHERE fs_kd_trs=@docId", new { docId });
    }

    private static void EnsureSchema()
    {
        lock (SchemaLock)
        {
            var opt = ConnStringHelper.GetTestEnv();
            using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
            conn.Open();
            var aptRoot = FindApotekSqlRoot();
            var salesRoot = FindSalesSqlRoot();
            EnsureTable(conn, aptRoot, "BILRG_AptSalesOrder", "BILRG_AptSalesOrder.sql");
            EnsureTable(conn, aptRoot, "BILRG_AptSalesOrderItem", "BILRG_AptSalesOrderItem.sql");
            EnsureTable(conn, aptRoot, "BILRG_AptInvoice", "BILRG_AptInvoice.sql");
            EnsureTable(conn, aptRoot, "BILRG_AptInvoiceItem", "BILRG_AptInvoiceItem.sql");
            EnsureTable(conn, salesRoot, "tb_trs_dobill_umum", "tb_trs_dobill_umum.sql");
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

    private static string FindApotekSqlRoot()
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

    private static string FindSalesSqlRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Bilreg.SqlDb", "SalesContext");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Unable to locate Bilreg.SqlDb/SalesContext.");
    }
}
