namespace Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;

public class PropinsiModel : IPropinsiKey
{
    private PropinsiModel(string id, string name) => (PropinsiId, PropinsiName) = (id, name);
    public static PropinsiModel Create(string id, string name) => new PropinsiModel(id, name);
    public string PropinsiId { get; private set; }
    public string PropinsiName { get; private set; }
}

