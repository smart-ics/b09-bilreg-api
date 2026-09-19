namespace Bilreg.Infrastructure.Shared.Helpers;

/// <summary>
/// SMASS service configuration (architecture §9.6, AR-04). Values are ops-owned and
/// default to empty; a missing value degrades the integration to a Failed task, not
/// a startup failure (no <c>ValidateOnStart</c>).
/// </summary>
public class SmassOptions
{
    public const string SECTION_NAME = "Smass";

    public string BaseApiUrl { get; set; } = string.Empty;
    public string TokenEmail { get; set; } = string.Empty;
    public string TokenPass { get; set; } = string.Empty;

    /// <summary>Request timeout in seconds; architecture default is 10 (range 1–120).</summary>
    public int TimeoutSeconds { get; set; } = 10;
}
