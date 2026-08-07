using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// Ensures P1-S5 Stock Ledger tables exist on the disposable/dev test database.
/// Never targets HOSPITAL_HPL.
/// </summary>
internal static class StockLedgerSchemaFixture
{
    private static readonly object Gate = new();
    private static bool _ensured;

    private static readonly string[] Scripts =
    [
        "BILRG_StokMovement.sql",
        "BILRG_StokMovementLine.sql",
        "BILRG_StokLayer.sql",
        "BILRG_StokPosition.sql",
        "BILRG_StokLedgerScope.sql",
        "BILRG_StokSourceIdempotency.sql"
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

            _ensured = true;
        }
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
