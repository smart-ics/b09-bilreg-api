using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;

namespace Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;

public class KarcisKomponenModel(string karcisId, 
    string komponenId, string komponenName, decimal nilai) : IKarcisKey, IKomponenKey
{
    public string KarcisId { get; protected set; } = karcisId;
    public string KomponenId { get; protected set; } = komponenId;
    public string KomponenName { get; protected set; } = komponenName;
    public decimal Nilai { get; protected set; } = nilai;
}