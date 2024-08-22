namespace Bilreg.Domain.AdmPasienContext.PendidikanDkAgg;

public class PendidikanDkModel: IPendidikanDkKey
{
    //  CONSTRUCTOR
    private PendidikanDkModel(string id, string name)
        => (PendidikanDkId, PendidikanDkName) = (id, name);

    //  FACTORY METHODS
    public static PendidikanDkModel Create(string id, string name) 
        => new PendidikanDkModel(id, name);
    
    //  PROPERTIES
    public string PendidikanDkId { get; private set; }
    public string PendidikanDkName { get; private set; }
}

public interface IPendidikanDkKey
{
    string PendidikanDkId {get;}
}