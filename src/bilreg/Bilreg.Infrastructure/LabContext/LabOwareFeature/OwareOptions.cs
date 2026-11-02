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
}
