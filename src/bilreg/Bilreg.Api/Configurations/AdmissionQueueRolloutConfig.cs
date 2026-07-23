using Bilreg.Application.AdmisiContext.AntrianFeature;
using Microsoft.Extensions.Options;

namespace Bilreg.Api.Configurations;

/// <summary>
/// Projects <see cref="AdmissionQueueApiOptions"/> into a key-free rollout preflight summary.
/// </summary>
public sealed class AdmissionQueueRolloutConfig : IAdmissionQueueRolloutConfig
{
    public AdmissionQueueRolloutConfig(IOptions<AdmissionQueueApiOptions> options)
    {
        var value = options.Value;
        LegacyEndpointsEnabled = value.LegacyEndpointsEnabled;
        SignalRRefreshEnabled = value.SignalRRefreshEnabled;
        var workstations = value.Workstations ?? [];
        WorkstationMappingCount = workstations.Count;
        WorkstationMappingsUnique =
            !workstations.GroupBy(x => x.WorkstationKey?.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
            && !workstations.GroupBy(x => x.LoketKey?.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1);
    }

    public bool LegacyEndpointsEnabled { get; }
    public bool SignalRRefreshEnabled { get; }
    public bool WorkstationMappingsUnique { get; }
    public int WorkstationMappingCount { get; }
}
