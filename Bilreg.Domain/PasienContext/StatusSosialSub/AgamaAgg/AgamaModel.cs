namespace Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;

public class AgamaModel : IAgamaKey
{
    public AgamaModel(string id, string name)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException("Invalid Agama");

        AgamaId = id;
        AgamaName = name;
    }
    public static AgamaModel Default => new AgamaModel(string.Empty, string.Empty);
    public AgamaModel()
    {
    }

    public string AgamaId { get; protected set; }
    public string AgamaName { get; protected set; }
}