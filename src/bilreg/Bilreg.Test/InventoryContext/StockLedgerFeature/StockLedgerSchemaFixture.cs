using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// Ensures Stock Ledger tables (and P2-S1 legacy read tables) exist on the disposable/dev test database.
/// Never targets HOSPITAL_HPL.
/// </summary>
internal static class StockLedgerSchemaFixture
{
    private static readonly object Gate = new();
    private static bool _ensured;
    private static bool _legacyEnsured;

    private static readonly string[] Scripts =
    [
        "BILRG_StokMovement.sql",
        "BILRG_StokMovementLine.sql",
        "BILRG_StokLayer.sql",
        "BILRG_StokPosition.sql",
        "BILRG_StokLedgerScope.sql",
        "BILRG_StokSourceIdempotency.sql",
        "BILRG_StokLayerLegacyBinding.sql"
    ];

    public static void EnsureSchema()
    {
        if (_ensured)
            return;

        lock (Gate)
        {
            if (_ensured)
                return;

            var options = ConnStringHelper.GetTestEnv().Value;
            RejectHospitalAuthorityDb(options.DbName);

            using var conn = new SqlConnection(ConnStringHelper.Get(options));
            conn.Open();

            var featurePath = Path.Combine(
                FindRepositoryRoot(),
                "src",
                "bilreg",
                "Bilreg.SqlDb",
                "InventoryContext",
                "StockLedgerFeature");

            foreach (var fileName in Scripts)
            {
                var script = File.ReadAllText(Path.Combine(featurePath, fileName));
                var batches = Regex.Split(
                    script,
                    @"^\s*GO\s*$",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);
                foreach (var batch in batches.Where(x => !string.IsNullOrWhiteSpace(x)))
                    conn.Execute(batch);
            }

            // P3-S4 / R-002 — widen IdempotencyKey on already-created test DBs.
            ApplyAlterScript(conn, Path.Combine(featurePath, "BILRG_StokSourceIdempotency.AlterIdempotencyKey.sql"));

            // P5-S3 — ensure coexistence binding table even when prior process created older schema.
            ApplyAlterScript(conn, Path.Combine(featurePath, "BILRG_StokLayerLegacyBinding.sql"));

            _ensured = true;
        }
    }

    /// <summary>
    /// P2-S1 — ensure authoritative legacy stock/journal tables exist on disposable DB
    /// so live read-adapter fixtures can seed without targeting HOSPITAL_HPL.
    /// </summary>
    public static void EnsureLegacyStockTables()
    {
        if (_legacyEnsured)
            return;

        lock (Gate)
        {
            if (_legacyEnsured)
                return;

            var options = ConnStringHelper.GetTestEnv().Value;
            RejectHospitalAuthorityDb(options.DbName);

            using var conn = new SqlConnection(ConnStringHelper.Get(options));
            conn.Open();

            var stokFeaturePath = Path.Combine(
                FindRepositoryRoot(),
                "src",
                "bilreg",
                "Bilreg.SqlDb",
                "InventoryContext",
                "StokFeature");

            EnsureTableIfMissing(conn, "tb_stok", Path.Combine(stokFeaturePath, "tb_stok.sql"));
            EnsureTableIfMissing(conn, "tb_buku", Path.Combine(stokFeaturePath, "tb_buku.sql"));

            _legacyEnsured = true;
        }
    }

    private static void EnsureTableIfMissing(SqlConnection conn, string tableName, string scriptPath)
    {
        var exists = conn.ExecuteScalar<int>(
            $"SELECT CASE WHEN OBJECT_ID(N'dbo.{tableName}', 'U') IS NULL THEN 0 ELSE 1 END");
        if (exists == 1)
            return;

        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Legacy stock schema script not found: {scriptPath}");

        ApplyAlterScript(conn, scriptPath);
    }

    private static void ApplyAlterScript(SqlConnection conn, string scriptPath)
    {
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Schema script not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);
        var batches = Regex.Split(
            script,
            @"^\s*GO\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var batch in batches.Where(x => !string.IsNullOrWhiteSpace(x)))
            conn.Execute(batch);
    }

    private static void RejectHospitalAuthorityDb(string dbName)
    {
        if (string.Equals(dbName, "HOSPITAL_HPL", StringComparison.OrdinalIgnoreCase)
            || string.Equals(dbName, "hospital_hpl", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Stock Ledger persistence tests must not run against HOSPITAL_HPL.");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(
                    directory.FullName,
                    "src",
                    "bilreg",
                    "Bilreg.SqlDb")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root containing src/bilreg/Bilreg.SqlDb was not found.");
    }
}
