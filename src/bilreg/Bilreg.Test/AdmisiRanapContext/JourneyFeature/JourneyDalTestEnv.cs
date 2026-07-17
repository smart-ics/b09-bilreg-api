using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// Shared env/schema gate for Journey DAL integration and volume suites.
/// Missing configuration or schema fails with a clear error (no soft-skip).
/// </summary>
public static class JourneyDalTestEnv
{
    public const string EnvServer = "BILREG_JOURNEY_IT_SERVER";
    public const string EnvDatabase = "BILREG_JOURNEY_IT_DATABASE";
    public const string EnvUser = "BILREG_JOURNEY_IT_USER";
    public const string EnvPassword = "BILREG_JOURNEY_IT_PASSWORD";

    public static (IOptions<DatabaseOptions> Opt, string ConnStr) RequireConfiguredConnection()
    {
        var server = Environment.GetEnvironmentVariable(EnvServer);
        var database = Environment.GetEnvironmentVariable(EnvDatabase);
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException(
                $"Journey DAL integration tests require {EnvServer} and {EnvDatabase}. " +
                "Unit runs must exclude Category=JourneyDalIntegration and Category=JourneyDalVolume. " +
                "Example: set BILREG_JOURNEY_IT_SERVER=dev.smart-ics.com and BILREG_JOURNEY_IT_DATABASE=<schema-complete-db>.");
        }

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

    public static void RequireSchema(string connStr)
    {
        try
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();
            conn.ExecuteScalar<int>("SELECT TOP 1 1 FROM BILRG_AdmAdmission");
            conn.ExecuteScalar<int>("SELECT TOP 1 1 FROM BILRG_AdmOpnameRequest");
            conn.ExecuteScalar<int>("SELECT TOP 1 1 FROM BILRG_AdmReservation");
            conn.ExecuteScalar<int>("SELECT TOP 1 1 FROM BILRG_BedWaitingList");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Journey DAL integration database is configured but the Admisi Ranap schema is missing or unreachable. " +
                $"Ensure BILRG_AdmAdmission / OpnameRequest / Reservation / BedWaitingList exist. Underlying error: {ex.Message}",
                ex);
        }
    }
}
