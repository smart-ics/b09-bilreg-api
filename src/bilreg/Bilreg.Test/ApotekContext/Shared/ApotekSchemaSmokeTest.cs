using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.Shared;

/// <summary>
/// APT-B30 SQL smoke: every production Apotek DDL script deploys to the test database.
/// </summary>
public class ApotekSchemaSmokeTest
{
    private static readonly object SchemaLock = new();

    private static readonly string[] TableScripts =
    [
        "BILRG_AptCopyResep.sql",
        "BILRG_AptCopyResepItem.sql",
        "BILRG_AptDispensing.sql",
        "BILRG_AptDispensingItem.sql",
        "BILRG_AptFinalReview.sql",
        "BILRG_AptIntegrationTask.sql",
        "BILRG_AptInvoice.sql",
        "BILRG_AptInvoiceItem.sql",
        "BILRG_AptInvoiceItemCharge.sql",
        "BILRG_AptJualBebas.sql",
        "BILRG_AptJualBebasItem.sql",
        "BILRG_AptQueueClose.sql",
        "BILRG_AptQueueMapping.sql",
        "BILRG_AptResepKerja.sql",
        "BILRG_AptResepKerjaComponent.sql",
        "BILRG_AptResepKerjaItem.sql",
        "BILRG_AptSalesOrder.sql",
        "BILRG_AptSalesOrderItem.sql",
        "BILRG_AptSalesOrderItemComponent.sql",
        "BILRG_AptTelaahResep.sql",
        "BILRG_AptTelaahResepItem.sql",
        "BILRG_AptUnfulfilledOutcome.sql",
    ];

    [Fact]
    public void All_apotek_table_scripts_exist_and_contain_create_table()
    {
        var sqlRoot = FindSqlRoot();
        foreach (var script in TableScripts)
        {
            var path = Path.Combine(sqlRoot, script);
            File.Exists(path).Should().BeTrue($"missing DDL script {script}");
            var text = File.ReadAllText(path);
            text.Should().Contain("CREATE TABLE", $"script {script} should define a table");
        }
    }

    [Fact]
    public void All_apotek_table_scripts_deploy_to_test_database()
    {
        lock (SchemaLock)
        {
            var opt = ConnStringHelper.GetTestEnv();
            using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
            conn.Open();
            var sqlRoot = FindSqlRoot();

            foreach (var script in TableScripts)
            {
                var tableName = Path.GetFileNameWithoutExtension(script);
                if (conn.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM sys.tables WHERE name = @tableName",
                        new { tableName }) != 0)
                    continue;

                ExecuteScript(conn, Path.Combine(sqlRoot, script));
                conn.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM sys.tables WHERE name = @tableName",
                        new { tableName })
                    .Should().Be(1, $"deploying {script} should create {tableName}");
            }
        }
    }

    [Fact]
    public void Collection_window_seed_script_exists_and_defaults_to_seven()
    {
        var sqlRoot = FindSqlRoot();
        var seed = File.ReadAllText(Path.Combine(sqlRoot, "BILRG_Apt_Seed_CollectionWindow.sql"));
        seed.Should().Contain("APT_COLLECTION_WINDOW_DAYS");
        seed.Should().Contain("'7'");
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
