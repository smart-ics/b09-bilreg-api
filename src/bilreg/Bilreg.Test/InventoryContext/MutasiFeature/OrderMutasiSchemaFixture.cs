using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public static class OrderMutasiSchemaFixture
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

            var featurePath = Path.Combine(
                FindRepositoryRoot(),
                "src",
                "bilreg",
                "Bilreg.SqlDb",
                "InventoryContext",
                "MutasiFeature");

            var script = File.ReadAllText(Path.Combine(featurePath, "FARIN_OrderMutasi_M1_ReqMutasiAlignment_Alter.sql"));
            var batches = Regex.Split(
                script,
                @"^\s*GO\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (var batch in batches.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                conn.Execute(batch);
            }

            _ensured = true;
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src", "bilreg", "Bilreg.SqlDb")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing src/bilreg/Bilreg.SqlDb was not found.");
    }
}
