namespace Bilreg.Api.Configurations;
public sealed class AdmissionQueueApiOptions
{
    public const string SectionName="AdmissionQueueApi";
    public bool LegacyEndpointsEnabled{get;set;}=true;
    /// <summary>
    /// When false, binds the no-op refresh publisher. Queue write truth is unchanged.
    /// </summary>
    public bool SignalRRefreshEnabled{get;set;}=true;
    public List<AdmissionQueueWorkstationOptions> Workstations{get;set;}=[];
    /// <summary>
    /// Usman role ids that grant AdmissionQueueConfiguration management access.
    /// </summary>
    public List<string> ConfigurationAllowedRoles{get;set;}=["ADM-SPV"];
    /// <summary>Roles allowed to perform terminal, supervisor-only queue operations.</summary>
    public List<string> SupervisorOperationAllowedRoles{get;set;}=["ADM-SPV"];
}

public sealed class AdmissionQueueWorkstationOptions
{
    public string WorkstationKey{get;set;}=string.Empty;
    public string LoketKey{get;set;}=string.Empty;
}
