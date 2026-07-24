using System.Reflection;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

/// <summary>
/// Fail-closed env gate for Admission Queue real-SQL Slice 1 / Phase 1.
/// Missing configuration throws — never soft-skip, never use shared GetTestEnv()/devTest.
/// </summary>
public static class AdmissionQueueRealSqlTestEnv
{
    public const string EnvServer = "BILREG_AQ_IT_SERVER";
    public const string EnvDatabase = "BILREG_AQ_IT_DATABASE";
    public const string EnvUser = "BILREG_AQ_IT_USER";
    public const string EnvPassword = "BILREG_AQ_IT_PASSWORD";
    public const string Category = "AdmissionQueueRealSql";

    public static (IOptions<DatabaseOptions> Opt, string ConnStr) RequireConfiguredConnection()
    {
        var server = Environment.GetEnvironmentVariable(EnvServer);
        var database = Environment.GetEnvironmentVariable(EnvDatabase);
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException(
                $"Admission Queue real-SQL tests require {EnvServer} and {EnvDatabase}. " +
                $"Unit runs must exclude Category={Category}. " +
                "Example: set BILREG_AQ_IT_SERVER=(local) and BILREG_AQ_IT_DATABASE=bilreg_aq_it. " +
                "Never point this gate at production.");
        }

        var forbidden = new[] { "prod", "production", "hospital_hpl" };
        if (forbidden.Any(f => database.Contains(f, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Refusing Admission Queue real-SQL target database '{database}'. Use a disposable integration database.");
        }

        // ConnStringHelper.Get always uses bilregLogin / bilreg123!; keep Options aligned.
        var user = Environment.GetEnvironmentVariable(EnvUser);
        var password = Environment.GetEnvironmentVariable(EnvPassword);
        if (string.IsNullOrWhiteSpace(user))
            user = "bilregLogin";
        if (string.IsNullOrWhiteSpace(password))
            password = "bilreg123!";

        var opt = Options.Create(new DatabaseOptions
        {
            ServerName = server.Trim(),
            DbName = database.Trim()
        });
        var connStr =
            $"Server={server.Trim()};Database={database.Trim()};User Id={user.Trim()};Password={password};";
        return (opt, connStr);
    }

    public static void ResetConnStringCache()
    {
        var field = typeof(ConnStringHelper).GetField("_connString",
            BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, string.Empty);
    }

    public static string ResolveSqlDbRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "bilreg", "Bilreg.SqlDb", "AdmisiContext");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate Bilreg.SqlDb/AdmisiContext relative to the test assembly.");
    }
}
