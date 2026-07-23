namespace Bilreg.Api.Configurations;
public sealed class AdmissionQueueApiOptions
{
    public const string SectionName="AdmissionQueueApi";
    public bool LegacyEndpointsEnabled{get;set;}=true;
    public List<AdmissionQueueWorkstationOptions> Workstations{get;set;}=[];
}

public sealed class AdmissionQueueWorkstationOptions
{
    public string WorkstationKey{get;set;}=string.Empty;
    public string LoketKey{get;set;}=string.Empty;
}
