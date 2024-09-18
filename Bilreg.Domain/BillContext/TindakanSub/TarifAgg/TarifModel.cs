using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;

namespace Bilreg.Domain.BillContext.TindakanSub.TarifAgg;

public class TarifModel(string id, string name) : ITarifKey
{
    public string TarifId { get; protected set; } = id;
    public string TarifName { get; protected set; } = name;
    public string RekapCetakId { get; protected set; }
    public string RekapCetakName { get; protected set; }
    public string GrupTarifDkId { get; protected set; }
    public string GrupTarifDkName { get; protected set; }
    public string GrupTarifId { get; protected set; }
    public string GrupTarifName { get; protected set; }
}

public interface ITarifKey
{
    string TarifId { get; }
}