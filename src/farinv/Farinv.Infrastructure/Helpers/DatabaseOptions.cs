namespace Farinv.Infrastructure.Helpers;

public class DatabaseOptions
{
    public const string SECTION_NAME = "Database";
    
    public string ServerName { get; set; } = string.Empty;
    public string DbName { get; set; } = string.Empty;
    public string DbTest { get; set; } = string.Empty;
}
