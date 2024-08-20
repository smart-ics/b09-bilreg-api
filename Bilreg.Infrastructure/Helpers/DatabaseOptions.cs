namespace Bilreg.Infrastructure.Helpers;

public class DatabaseOptions
{
    public const string SECTION_NAME = "Database";

    public string ServerName { get; set; }
    public string DbName { get; set; }
}