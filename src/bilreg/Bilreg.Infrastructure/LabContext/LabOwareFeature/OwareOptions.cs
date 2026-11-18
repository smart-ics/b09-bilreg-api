namespace Bilreg.Infrastructure.LabContext.LabOwareFeature;

public class OwareOptions
{
    public const string SECTION_NAME = "Oware";

    public string BaseUrl { get; set; } = string.Empty;
    public string OrderLabEndpoint { get; set; } = "/api/order/lab/bilreg";
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxRetryCount { get; set; } = 3;
    public bool WorkerEnabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 15;

    public bool IsConfiguredAndValid =>
        !string.IsNullOrWhiteSpace(BaseUrl) &&
        Uri.TryCreate(BaseUrl.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        !string.IsNullOrWhiteSpace(uri.Host);
}
