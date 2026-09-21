namespace Bilreg.Application.IgdContext;

/// <summary>
/// IGD Visit integration configuration (architecture §9.6, AR-01, AR-02, D-06).
/// Values are ops-owned and intentionally default to "off"/empty so that a
/// half-configured deployment still serves IGD triage (no <c>ValidateOnStart</c>).
/// </summary>
public class IgdVisitOptions
{
    public const string SECTION_NAME = "IgdVisit";

    /// <summary>
    /// AR-01 — master toggle for the SMASS integration. Default <c>false</c>:
    /// with the toggle off no task row is created and no HTTP call is made.
    /// </summary>
    public bool EnableSmassIntegration { get; set; } = false;

    /// <summary>
    /// D-06 / AR-02 — configured IGD <c>LayananId</c> used when generating the
    /// SMASS assessment. Empty at call time → generation task Failed, no HTTP call.
    /// </summary>
    public string SmassLayananId { get; set; } = string.Empty;

    /// <summary>
    /// D-05 / AR-09 — dedicated IGD Triage Paper id (mirrored from the SMASS seed,
    /// e.g. <c>PP-001-IGDT</c>; deliberately not hard-coded here). Empty at call
    /// time → generation task Failed, no HTTP call.
    /// </summary>
    public string SmassTriagePaperId { get; set; } = string.Empty;
}
