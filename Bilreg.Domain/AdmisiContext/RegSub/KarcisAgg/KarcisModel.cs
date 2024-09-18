using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Domain.BillContext.TindakanSub.TarifAgg;

namespace Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;

public partial class KarcisModel(string id, string name) : IKarcisKey, IInstalasiDkKey, ITarifKey
{
    public string KarcisId { get; protected set; }
    public string KarcisName { get; protected set; }
    
    public decimal Nilai { get => ListKomponen.Sum(x => x.Nilai); }
    
    public string InstalasiDkId { get; protected set; }
    public string InstalasiDkName { get; protected set; }
    
    public string RekapCetakId { get; protected set; }
    public string TarifId { get; protected set; }
    public string TarifName { get; protected set; }
    
    public bool IsAktif { get; protected set; }

    public List<KarcisKomponenModel> ListKomponen { get; protected set; } = [];
    public List<KarcisLayananModel> ListLayanan { get; protected set; } = [];
}

