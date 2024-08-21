namespace Bilreg.Domain.AdmPasienContext.PendidikanDKAgg;

public class PendidikanDKModel: IPendidikanDKKey
{
    //  CONSTRUCTOR
    private PendidikanDKModel(string id, string name)
        => (PendidikanDKId, PendidikanDKName) = (id, name);

    //  FACTORY METHODS
    public static PendidikanDKModel Create(string id, string name) 
        => new PendidikanDKModel(id, name);
    
    //  PROPERTIES
    public string PendidikanDKId { get; private set; }
    public string PendidikanDKName { get; private set; }
}

public interface IPendidikanDKKey
{
    string PendidikanDKId {get;}
}