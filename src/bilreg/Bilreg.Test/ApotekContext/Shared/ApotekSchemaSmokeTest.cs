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
        "BILRG_AptDisplayLoket.sql",
        "BILRG_AptFinalReview.sql",
        "BILRG_AptIntegrationTask.sql",
        "BILRG_AptInvoice.sql",
        "BILRG_AptInvoiceItem.sql",
        "BILRG_AptInvoiceItemCharge.sql",
        "BILRG_AptJualBebas.sql",
        "BILRG_AptJualBebasItem.sql",
        "BILRG_AptLoketCurrentCall.sql",
        "BILRG_AptLoketServicePoint.sql",
        "BILRG_AptQueueClose.sql",
        "BILRG_AptQueueEntry.sql",
        "BILRG_AptQueueMapping.sql",
        "BILRG_AptSalesOrder.sql",
        "BILRG_AptSalesOrderItem.sql",
        "BILRG_AptSalesOrderItemComponent.sql",
        "BILRG_AptServicePoint.sql",
        "BILRG_AptTelaahResep.sql",
        "BILRG_AptTelaahResepItem.sql",
        "BILRG_AptTelaahResepKajian.sql",
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

            EnsureTablesDeployed(conn, sqlRoot);

            foreach (var script in TableScripts)
            {
                var tableName = Path.GetFileNameWithoutExtension(script);
                conn.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM sys.tables WHERE name = @tableName",
                        new { tableName })
                    .Should().Be(1, $"deploying {script} should create {tableName}");
            }
        }
    }

    private static void EnsureTablesDeployed(SqlConnection conn, string sqlRoot)
    {
        foreach (var script in TableScripts)
        {
            var tableName = Path.GetFileNameWithoutExtension(script);
            if (conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM sys.tables WHERE name = @tableName",
                    new { tableName }) != 0)
                continue;

            ExecuteScript(conn, Path.Combine(sqlRoot, script));
        }
    }

    [Fact]
    public void AptQueueMapping_alter_pasien_hadir_deploys_to_test_database()
    {
        lock (SchemaLock)
        {
            var opt = ConnStringHelper.GetTestEnv();
            using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
            conn.Open();
            var sqlRoot = FindSqlRoot();

            var alterPath = Path.Combine(sqlRoot, "BILRG_AptQueueMapping_Alter_PasienHadir.sql");
            File.Exists(alterPath).Should().BeTrue("BILRG_AptQueueMapping_Alter_PasienHadir.sql should exist");

            ExecuteScript(conn, alterPath);

            var colExists = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AptQueueMapping') AND name = 'PasienHadir'");
            colExists.Should().Be(1, "PasienHadir column should exist in BILRG_AptQueueMapping");

            var colAtExists = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AptQueueMapping') AND name = 'PasienHadirAt'");
            colAtExists.Should().Be(1, "PasienHadirAt column should exist in BILRG_AptQueueMapping");
        }
    }

    [Fact]
    public void Pharmacy_queue_tables_are_deployed_identically_to_test_database()
    {
        lock (SchemaLock)
        {
            var opt = ConnStringHelper.GetTestEnv();
            using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
            conn.Open();
            var sqlRoot = FindSqlRoot();
            EnsureTablesDeployed(conn, sqlRoot);

            var expectedTables = new[]
            {
                "BILRG_AptServicePoint",
                "BILRG_AptQueueEntry",
                "BILRG_AptLoketCurrentCall",
                "BILRG_AptDisplayLoket",
                "BILRG_AptLoketServicePoint"
            };

            foreach (var table in expectedTables)
            {
                var count = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM sys.tables WHERE name = @table",
                    new { table });
                count.Should().Be(1, $"{table} should exist in test database");
            }

            var pasienHadirCount = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AptQueueMapping') AND name = 'PasienHadir'");
            pasienHadirCount.Should().Be(1, "BILRG_AptQueueMapping must have PasienHadir column");

            var pasienHadirAtCount = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AptQueueMapping') AND name = 'PasienHadirAt'");
            pasienHadirAtCount.Should().Be(1, "BILRG_AptQueueMapping must have PasienHadirAt column");

            var aptPhaseCount = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_AptQueueEntry') AND name = 'ApotekPhase'");
            aptPhaseCount.Should().Be(1, "BILRG_AptQueueEntry must have ApotekPhase column");

            var rowVersionType = conn.ExecuteScalar<string>(
                """
                SELECT t.name 
                FROM sys.columns c 
                JOIN sys.types t ON c.user_type_id = t.user_type_id 
                WHERE c.object_id = OBJECT_ID('BILRG_AptLoketCurrentCall') AND c.name = 'RowVersion'
                """);
            rowVersionType.Should().Be("timestamp", "BILRG_AptLoketCurrentCall must have RowVersion of type timestamp/rowversion");
        }
    }

    [Fact]
    public void Service_points_jaminan_seed_script_exists_and_deploys_to_test_database()
    {
        lock (SchemaLock)
        {
            var opt = ConnStringHelper.GetTestEnv();
            using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
            conn.Open();
            var sqlRoot = FindSqlRoot();
            EnsureTablesDeployed(conn, sqlRoot);

            var seedPath = Path.Combine(sqlRoot, "BILRG_Apt_Seed_ServicePoints_Jaminan.sql");
            File.Exists(seedPath).Should().BeTrue("BILRG_Apt_Seed_ServicePoints_Jaminan.sql should exist");

            ExecuteScript(conn, seedPath);

            var spBpjs = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM BILRG_AptServicePoint WHERE ServicePointId = 'SP-FAR-BPJS' AND QueuePrefix = 'A'");
            spBpjs.Should().Be(1, "SP-FAR-BPJS with prefix A should exist");

            var spUmum = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM BILRG_AptServicePoint WHERE ServicePointId = 'SP-FAR-UMUM' AND QueuePrefix = 'B'");
            spUmum.Should().Be(1, "SP-FAR-UMUM with prefix B should exist");

            var loket = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM BILRG_AptLoketCurrentCall WHERE LoketKey = 'APT-RCV-01'");
            loket.Should().Be(1, "Default loket APT-RCV-01 should exist");

            var mappingBpjs = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM BILRG_AptLoketServicePoint WHERE LoketKey = 'APT-RCV-01' AND ServicePointId = 'SP-FAR-BPJS'");
            mappingBpjs.Should().Be(1, "APT-RCV-01 to SP-FAR-BPJS mapping should exist");

            var mappingUmum = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM BILRG_AptLoketServicePoint WHERE LoketKey = 'APT-RCV-01' AND ServicePointId = 'SP-FAR-UMUM'");
            mappingUmum.Should().Be(1, "APT-RCV-01 to SP-FAR-UMUM mapping should exist");
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
