namespace Bilreg.Infrastructure.Shared.Helpers;

/// <summary>
/// EMR 2.0 service configuration (architecture TD-03).
/// </summary>
public class Emr20Options
{
    public const string SECTION_NAME = "Emr20";

    public string BaseApiUrl { get; set; } = string.Empty;
}
