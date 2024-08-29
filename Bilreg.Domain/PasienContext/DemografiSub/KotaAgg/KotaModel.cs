namespace Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;

public class KotaModel: IKotaKey
{
    public KotaModel(string id, string name) => (KotaId, KotaName) = (id, name);
    public static KotaModel Create(string id, string name) => new KotaModel(id, name);
    public string KotaId { get; private set; }
    public string KotaName { get; private set; }
}

public interface IKotaKey
{
    string KotaId { get; }
}