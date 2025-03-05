namespace Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;

public class PropinsiModel : IPropinsiKey
{
    public PropinsiModel(string id, string name)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException("Invalid Propinsi");
        
        PropinsiId = id;
        PropinsiName = name;
    }

    public static PropinsiModel Default => new PropinsiModel(string.Empty, string.Empty);
    
    public PropinsiModel()
    {
    }
    public string PropinsiId { get; private set; }
    public string PropinsiName { get; private set; }
}

