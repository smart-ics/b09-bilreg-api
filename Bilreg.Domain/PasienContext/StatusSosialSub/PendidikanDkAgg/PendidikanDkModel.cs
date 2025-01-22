namespace Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;

public class PendidikanDkModel: IPendidikanDkKey
{
    //  CONSTRUCTOR
    public PendidikanDkModel(string id, string name)
        => (PendidikanDkId, PendidikanDkName) = (id, name);

    public PendidikanDkModel()
    {
    }
    
    //  PROPERTIES
    public string PendidikanDkId { get; private set; }
    public string PendidikanDkName { get; private set; }
}