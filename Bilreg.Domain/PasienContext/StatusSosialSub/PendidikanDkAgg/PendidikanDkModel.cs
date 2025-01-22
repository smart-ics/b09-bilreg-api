namespace Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;

public class PendidikanDkModel: IPendidikanDkKey
{
    public PendidikanDkModel(string id, string name)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException("Invalid PendidikanDk");
        
        PendidikanDkId = id;
        PendidikanDkName = name;
    }
    
    public static PendidikanDkModel Default => new PendidikanDkModel(string.Empty, string.Empty);

    public PendidikanDkModel()
    {
    }
    
    public string PendidikanDkId { get; private set; }
    public string PendidikanDkName { get; private set; }
}