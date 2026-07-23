using System.Data.SqlClient;
using System.Text;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

/// <summary>
/// Applies R-06 through R-13 Admission Queue scripts in dependency order to a disposable DB.
/// </summary>
public sealed class AdmissionQueueMigrationFixture : IDisposable
{
    public IOptions<DatabaseOptions> Options { get; }
    public string ConnectionString { get; }
    public IReadOnlyList<string> AppliedScriptOrder { get; private set; } = [];
    public string DatabaseVersion { get; private set; } = "";
    public string SqlDbRoot { get; }

    private static readonly (string RelativePath, string? GuardTable)[] Scripts =
    [
        ("AntrianFeature/BILRG_Antrian.sql", "BILRG_Antrian"),
        ("AntrianFeature/BILRG_AntrianEntry.sql", "BILRG_AntrianEntry"),
        ("AntrianFeature/BILRG_Antrian_M1_ServicePointCode_Alter.sql", null),
        ("AntrianFeature/BILRG_Antrian_M2_QueuePrefixSnapshot_Alter.sql", null),
        ("AntrianFeature/BILRG_AntrianEntry_M2_QueueOperations_Alter.sql", null),
        ("AntrianFeature/BILRG_AdmServicePoint.sql", "BILRG_AdmServicePoint"),
        ("AntrianFeature/BILRG_AdmLoketCurrentCall.sql", "BILRG_AdmLoketCurrentCall"),
        ("RegFeature/BILRG_RegOutcome.sql", "BILRG_RegOutcome"),
        ("AntrianFeature/BILRG_AdmBookingAssistance.sql", "BILRG_AdmBookingAssistance"),
        ("AntrianFeature/BILRG_AdmissionQueue_M3_Audit_Alter.sql", null),
    ];

    public AdmissionQueueMigrationFixture()
    {
        AdmissionQueueRealSqlTestEnv.ResetConnStringCache();
        var (opt, connStr) = AdmissionQueueRealSqlTestEnv.RequireConfiguredConnection();
        Options = opt;
        ConnectionString = connStr;
        SqlDbRoot = AdmissionQueueRealSqlTestEnv.ResolveSqlDbRoot();
        // Force ConnStringHelper to resolve against the disposable target.
        _ = ConnStringHelper.Get(opt.Value);
        ApplyMigrations();
        ProbeVersion();
        RequireSchema();
        SeedServicePoints();
    }

    public void Dispose() { }

    private void ApplyMigrations()
    {
        var applied = new List<string>();
        foreach (var (relative, guard) in Scripts)
        {
            var fullPath = Path.Combine(SqlDbRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
                throw new InvalidOperationException($"Admission Queue migration script missing: {fullPath}");

            if (guard is not null && TableExists(guard))
            {
                applied.Add($"{relative} (skipped — {guard} exists)");
                continue;
            }

            ExecuteScriptFile(fullPath);
            applied.Add(relative);
        }

        AppliedScriptOrder = applied;
    }

    private void RequireSchema()
    {
        try
        {
            using var conn = Open();
            conn.Open();
            foreach (var table in new[]
                     {
                         "BILRG_Antrian", "BILRG_AntrianEntry", "BILRG_AdmServicePoint",
                         "BILRG_AdmLoketCurrentCall", "BILRG_RegOutcome", "BILRG_AdmBookingAssistance"
                     })
            {
                conn.ExecuteScalar<int>($"SELECT TOP 1 1 FROM {table} WHERE 1=0");
            }

            var hasUnique = conn.ExecuteScalar<int>("""
                SELECT COUNT(1) FROM sys.indexes
                WHERE name = 'UX_BILRG_Antrian_SequenceTag'
                  AND object_id = OBJECT_ID('BILRG_Antrian')
                """);
            if (hasUnique != 1)
                throw new InvalidOperationException("UX_BILRG_Antrian_SequenceTag is missing after migration.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Admission Queue real-SQL database is configured but schema is incomplete or unreachable. " +
                $"Underlying error: {ex.Message}", ex);
        }
    }

    private void SeedServicePoints()
    {
        using var conn = Open();
        conn.Open();
        conn.Execute("""
            IF NOT EXISTS (SELECT 1 FROM BILRG_AdmServicePoint WHERE ServicePointId = 'AQIT-A')
              INSERT BILRG_AdmServicePoint(ServicePointId, DisplayName, QueuePrefix, ServicePointStatus)
              VALUES('AQIT-A', 'AQ IT Loket A', 'A', 1);
            IF NOT EXISTS (SELECT 1 FROM BILRG_AdmServicePoint WHERE ServicePointId = 'AQIT-B')
              INSERT BILRG_AdmServicePoint(ServicePointId, DisplayName, QueuePrefix, ServicePointStatus)
              VALUES('AQIT-B', 'AQ IT Loket B', 'B', 1);
            """);
    }

    private void ProbeVersion()
    {
        using var conn = Open();
        conn.Open();
        DatabaseVersion = conn.ExecuteScalar<string>("SELECT CONVERT(NVARCHAR(200), SERVERPROPERTY('ProductVersion'))")
                          ?? "";
        var edition = conn.ExecuteScalar<string>("SELECT CONVERT(NVARCHAR(200), SERVERPROPERTY('Edition'))");
        DatabaseVersion = $"{DatabaseVersion} ({edition}); DB={Options.Value.DbName}; Server={Options.Value.ServerName}";
    }

    private bool TableExists(string tableName)
    {
        using var conn = Open();
        conn.Open();
        return conn.ExecuteScalar<int>(
            "SELECT CASE WHEN OBJECT_ID(@name, 'U') IS NULL THEN 0 ELSE 1 END",
            new { name = tableName }) == 1;
    }

    private void ExecuteScriptFile(string path)
    {
        var text = File.ReadAllText(path, Encoding.UTF8);
        foreach (var batch in SplitGoBatches(text))
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;
            using var conn = Open();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = batch;
            cmd.CommandTimeout = 120;
            cmd.ExecuteNonQuery();
        }
    }

    private static IEnumerable<string> SplitGoBatches(string script)
    {
        var batches = new List<string>();
        var current = new StringBuilder();
        using var reader = new StringReader(script);
        while (reader.ReadLine() is { } line)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                batches.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.AppendLine(line);
            }
        }

        if (current.Length > 0)
            batches.Add(current.ToString());
        return batches;
    }

    private SqlConnection Open() => new(ConnectionString);
}

[CollectionDefinition("AdmissionQueueRealSqlDb", DisableParallelization = true)]
public sealed class AdmissionQueueRealSqlCollection : ICollectionFixture<AdmissionQueueMigrationFixture>;
