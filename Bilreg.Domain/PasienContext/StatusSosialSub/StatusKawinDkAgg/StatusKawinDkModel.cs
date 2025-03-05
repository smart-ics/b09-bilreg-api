using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;

public class StatusKawinDkModel : IStatusKawinDkKey
{
    public StatusKawinDkModel(string id, string name)
    {
        if (id == string.Empty ^ name == string.Empty) 
            throw new ArgumentException(("Invalid status kawin dk."));
        
        StatusKawinDkId = id;
        StatusKawinDkName = name;
    }
    public static StatusKawinDkModel Default => new(string.Empty, string.Empty);

    public StatusKawinDkModel()
    {
    }
    public string StatusKawinDkId { get; private set; }
    public string StatusKawinDkName { get; private set; }
}