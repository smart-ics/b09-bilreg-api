using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Infrastructure.ApotekContext.InvoiceFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.InvoiceFeature;

public class InvoiceDalTest
{
    private static readonly object SchemaLock = new();

    private readonly InvoiceDal _dal;
    private readonly InvoiceItemDal _itemDal;
    private readonly InvoiceChargeDal _chargeDal;
    private readonly InvoiceRepo _repo;

    public InvoiceDalTest()
    {
        EnsureSchema();
        var opt = ConnStringHelper.GetTestEnv();
        _dal = new(opt);
        _itemDal = new(opt);
        _chargeDal = new(opt);
        _repo = new(_dal, _itemDal, _chargeDal);
    }

    [Fact]
    public void RoundTrip_preserves_header_items_and_charges()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var salesOrderId = $"ASO{run}";
        var snapshot = new DateTime(2026, 8, 27, 9, 0, 0);
        var invoice = InvoiceModel.Establish(
            salesOrderId,
            PayerPathEnum.GeneralPatientPay,
            snapshot,
            "UMUM",
            "Umum",
            snapshot,
            [new InvoiceItemModel(1, 1, $"BRG{run}", "Obat", InvoiceItemKindEnum.Medication, 3, 100, 0, 5, 10, 300)],
            [new InvoiceItemChargeModel(1, 1, "Packaging", 15)],
            diskonLain: 2,
            biayaLain: 4,
            pembulatan: 1);

        _repo.SaveChanges(invoice);

        var loaded = _repo.LoadEntity(InvoiceModel.Key(invoice.InvoiceId)).Value;
        loaded.SalesOrderId.Should().Be(salesOrderId);
        loaded.PayerPath.Should().Be(PayerPathEnum.GeneralPatientPay);
        loaded.PricingSnapshotAt.Should().Be(snapshot);
        loaded.InvoiceStatus.Should().Be(InvoiceStatusEnum.Established);
        loaded.Items.Should().ContainSingle();
        loaded.Items[0].SalesOrderItemNo.Should().Be(1);
        loaded.Items[0].BrgId.Should().Be($"BRG{run}");
        loaded.Items[0].Qty.Should().Be(3m);
        loaded.Charges.Should().ContainSingle();
        loaded.Charges[0].ChargeName.Should().Be("Packaging");
        loaded.Charges[0].Amount.Should().Be(15m);
        loaded.DiskonLain.Should().Be(2m);
        loaded.BiayaLain.Should().Be(4m);
        loaded.Pembulatan.Should().Be(1m);
        loaded.GrandTotal.Should().Be(333m);

        Cleanup(invoice.InvoiceId);
    }

    [Fact]
    public void Issue_update_persists_status_and_version()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var invoice = InvoiceModel.Establish(
            $"ASO{run}",
            PayerPathEnum.GeneralPatientPay,
            DateTime.Now,
            "UMUM",
            "Umum",
            DateTime.Now,
            [new InvoiceItemModel(1, 1, $"BRG{run}", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [],
            0, 0, 0);
        Cleanup(invoice.InvoiceId);
        _repo.SaveChanges(invoice);

        invoice.Issue(DateTime.Now);
        _repo.SaveChanges(invoice);

        var loaded = _repo.LoadEntity(invoice).Value;
        loaded.InvoiceStatus.Should().Be(InvoiceStatusEnum.Issued);
        loaded.Version.Should().Be(2);
        loaded.IssuedAt.Should().NotBe(default);

        Cleanup(invoice.InvoiceId);
    }

    private static void EnsureSchema()
    {
        lock (SchemaLock)
        {
            var opt = ConnStringHelper.GetTestEnv();
            using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
            conn.Open();

            var sqlRoot = FindSqlRoot();
            EnsureTable(conn, sqlRoot, "BILRG_AptInvoice", "BILRG_AptInvoice.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptInvoiceItem", "BILRG_AptInvoiceItem.sql");
            EnsureTable(conn, sqlRoot, "BILRG_AptInvoiceItemCharge", "BILRG_AptInvoiceItemCharge.sql");
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

    private void Cleanup(params string[] ids)
    {
        var opt = ConnStringHelper.GetTestEnv();
        using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
        foreach (var id in ids)
        {
            conn.Execute("DELETE FROM BILRG_AptInvoiceItemCharge WHERE InvoiceId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptInvoiceItem WHERE InvoiceId=@id", new { id });
            conn.Execute("DELETE FROM BILRG_AptInvoice WHERE InvoiceId=@id", new { id });
        }
    }
}
