namespace Bilreg.Infrastructure.Shared.Helpers;

public class JknOptions
{
    public const string SECTION_NAME = "Jkn";
    public string BaseApiUrl { get; set; } = string.Empty;
    public bool IsSendJadwalToHfis { get; set; } = false;
    public string ConsId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
