namespace Bilreg.Application.AdmisiRanapContext;

public class AdmisiRanapOptions
{
    public const string SECTION_NAME = "AdmisiRanap";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Coordinated Release 1 rollout gate for the journey read APIs.  This is deliberately
    /// independent from the established module gate so existing record-oriented endpoints
    /// remain available while consumers migrate.
    /// </summary>
    public bool JourneyEndpointsEnabled { get; set; }
}
