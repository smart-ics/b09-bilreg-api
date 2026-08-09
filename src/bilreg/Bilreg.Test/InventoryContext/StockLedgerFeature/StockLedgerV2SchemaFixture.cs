using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// Ensures the five Stock Ledger v2 tables exist on the test database by applying
/// CREATE scripts from Bilreg.SqlDb when missing.
/// </summary>
public static class StockLedgerV2SchemaFixture
{
    private static readonly object Gate = new();
    private static bool _ensured;

    public static void EnsureSchema()
    {
        if (_ensured)
            return;

        lock (Gate)
        {
            if (_ensured)
                return;

            var options = ConnStringHelper.GetTestEnv().Value;
            using var conn = new SqlConnection(ConnStringHelper.Get(options));
            conn.Open();

            ExecuteScriptWhenMissing(conn, "BILRG_StokBatch", "BILRG_StokBatch.sql");
            ExecuteScriptWhenMissing(conn, "BILRG_StokLokasi", "BILRG_StokLokasi.sql");
            ExecuteScriptWhenMissing(conn, "BILRG_StokMutasi", "BILRG_StokMutasi.sql");
            ExecuteScriptWhenMissing(conn, "BILRG_StokLegacyScope", "BILRG_StokLegacyScope.sql");
            ExecuteScriptWhenMissing(conn, "BILRG_StokLegacyBinding", "BILRG_StokLegacyBinding.sql");

            _ensured = true;
        }
    }

    private static void ExecuteScriptWhenMissing(
        SqlConnection conn,
        string tableName,
        string fileName)
    {
        var exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM sys.tables WHERE name = @TableName",
            new { TableName = tableName });
        if (exists != 0)
            return;

        var featurePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "bilreg",
            "Bilreg.SqlDb",
            "InventoryContext",
            "StockLedgerFeature");
        var script = File.ReadAllText(Path.Combine(featurePath, fileName));
        var batches = Regex.Split(
            script,
            @"^\s*GO\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var batch in batches.Where(x => !string.IsNullOrWhiteSpace(x)))
            conn.Execute(batch);
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
