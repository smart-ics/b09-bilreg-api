namespace Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;

public class PropinsiModel : IPropinsiKey
{
    public PropinsiModel(string id, string name)
    {
        PropinsiId = id;
        PropinsiName = name;
    }

    public PropinsiModel()
    {
    }
    public string PropinsiId { get; private set; }
    public string PropinsiName { get; private set; }
}

