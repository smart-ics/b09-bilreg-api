namespace Bilreg.Domain.BillContext.TindakanSub.GrupTarifDkAgg;

public class GrupTarifDkModel(string id, string name) : IGrupTarifDkKey 
{
    public string GrupTarifDkId { get; protected set; }
    public string GrupTarifDkName { get; protected set; }
}