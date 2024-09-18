namespace Bilreg.Domain.BillContext.TindakanSub.GrupTarifAgg;

public class GrupTarifModel(string id, string name) : IGrupTarifKey
{
    public string GrupTarifId { get; protected set; } = id;
    public string GrupTarifName { get; protected set; } = name;
}