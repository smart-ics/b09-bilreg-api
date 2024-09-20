using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Domain.BillContext.TindakanSub.TarifAgg;

namespace Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;

public partial class KarcisModel(string id, string name) : IKarcisKey, IInstalasiDkKey, ITarifKey
{
    public string KarcisId { get; protected set; } = id;
    public string KarcisName { get; protected set; } = name;
    
    public decimal Nilai { get => ListKomponen.Sum(x => x.Nilai); }

    public string InstalasiDkId { get; protected set; } = string.Empty;
    public string InstalasiDkName { get; protected set; } = string.Empty;
    
    public string RekapCetakId { get; protected set; } = string.Empty;
    public string RekapCetakName { get; protected set; } = string.Empty;
    public string TarifId { get; protected set; } = string.Empty;
    public string TarifName { get; protected set; } = string.Empty;

    public bool IsAktif { get; protected set; } = false;

    public List<KarcisKomponenModel> ListKomponen { get; protected set; } = [];
    public List<KarcisLayananModel> ListLayanan { get; protected set; } = [];
}

