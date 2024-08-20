using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.Helpers;

public static class ConnStringHelper
{
    private static string _connString = string.Empty;
    private const string USER_ID = "bilregLogin";
    private const string PASS = "bilreg123!";
    
    public static string Get(DatabaseOptions options)
    {
        if (_connString.Length == 0)
            _connString = Generate(options.ServerName, options.DbName);

        return _connString;
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
            DbName = "devTest"
        });
        return result;
    }
}