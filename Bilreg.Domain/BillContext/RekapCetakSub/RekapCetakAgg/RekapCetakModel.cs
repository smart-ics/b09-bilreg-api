using Bilreg.Domain.BillContext.RekapCetakSub.GrupRekapCetakAgg;

namespace Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;

public class RekapCetakModel(string id, string name) : IRekapCetakKey
{
    public string RekapCetakId { get; protected set; } = id;
    public string RekapCetakName { get; protected set; } = name;
    public int NoUrut { get; protected set; }
    public bool IsGrupBaru { get; protected set; }
    public int Level { get; protected set; }
    public string GrupRekapCetakId { get; protected set; }
    public string GrupRekapCetakName { get; protected set; }
    public string RekapCetakDkId { get; protected set; }
    public string RekapCetakDkName { get; protected set; }
}