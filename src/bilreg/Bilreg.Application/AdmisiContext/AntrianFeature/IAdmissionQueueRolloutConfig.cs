namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Feature-flag / workstation summary for rollout preflight. Does not expose workstation or Loket keys.
/// </summary>
public interface IAdmissionQueueRolloutConfig
{
    bool LegacyEndpointsEnabled { get; }
    bool SignalRRefreshEnabled { get; }
    bool WorkstationMappingsUnique { get; }
    int WorkstationMappingCount { get; }
}
