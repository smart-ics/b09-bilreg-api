namespace Bilreg.Infrastructure.AdmisiRanapContext.DigitalSignFeature;

public class OftaOptions
{
    public const string SECTION_NAME = "Ofta";

    public string BaseApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ServiceToken { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
