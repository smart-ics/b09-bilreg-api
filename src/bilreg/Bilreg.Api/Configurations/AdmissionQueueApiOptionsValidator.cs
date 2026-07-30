using Microsoft.Extensions.Options;

namespace Bilreg.Api.Configurations;

public sealed class AdmissionQueueApiOptionsValidator : IValidateOptions<AdmissionQueueApiOptions>
{
    public ValidateOptionsResult Validate(string? name, AdmissionQueueApiOptions options)
    {
        var errors=new List<string>();
        var workstations=options.Workstations ?? [];
        foreach(var workstation in workstations)
        {
            if(string.IsNullOrWhiteSpace(workstation.WorkstationKey)) errors.Add("AdmissionQueueApi:Workstations:WorkstationKey is required.");
            if(string.IsNullOrWhiteSpace(workstation.LoketKey)) errors.Add("AdmissionQueueApi:Workstations:LoketKey is required.");
            if(workstation.WorkstationKey?.Trim().Length>50) errors.Add("AdmissionQueueApi:Workstations:WorkstationKey may not exceed 50 characters.");
            if(workstation.LoketKey?.Trim().Length>50) errors.Add("AdmissionQueueApi:Workstations:LoketKey may not exceed 50 characters.");
        }
        if(workstations.GroupBy(x=>x.WorkstationKey?.Trim(),StringComparer.OrdinalIgnoreCase).Any(x=>!string.IsNullOrWhiteSpace(x.Key)&&x.Count()>1))
            errors.Add("AdmissionQueueApi:Workstations contains a duplicate WorkstationKey.");
        if(workstations.GroupBy(x=>x.LoketKey?.Trim(),StringComparer.OrdinalIgnoreCase).Any(x=>!string.IsNullOrWhiteSpace(x.Key)&&x.Count()>1))
            errors.Add("AdmissionQueueApi:Workstations contains a duplicate LoketKey.");
        return errors.Count==0?ValidateOptionsResult.Success:ValidateOptionsResult.Fail(errors);
    }
}
