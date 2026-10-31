using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.Shared.Helpers;

public static class ConnStringHelper
{
    private static string _connString = string.Empty;
    private const string USER_ID = "bilregLogin";
    private const string PASS = "bilreg123!";
    
    public static string Get(DatabaseOptions options)
    {
        return Generate(options.ServerName, options.DbName);
    }

    private static string Generate(string server, string db)
    {
        var result = $"Server={server};Database={db};User Id={USER_ID};Password={PASS};";
        return result;
    }

    public static IOptions<DatabaseOptions> GetTestEnv()
    {
        var result = Options.Create<DatabaseOptions>(new DatabaseOptions
        {
            ServerName = "dev.smart-ics.com",
            DbName = "HOSPITAL_PKL"
            // ServerName = "(local)",
            // DbName = "devTest"

        });
        return result;
    }
}