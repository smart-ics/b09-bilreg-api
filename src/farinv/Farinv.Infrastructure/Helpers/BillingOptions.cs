namespace Farinv.Infrastructure.Helpers;

public class BillingOptions
{
    public const string SECTION_NAME = "Billing";

    public string BaseApiUrl { get; set; }
    public string TokenEmail { get; set; }
    public string TokenPass { get; set; }
}
