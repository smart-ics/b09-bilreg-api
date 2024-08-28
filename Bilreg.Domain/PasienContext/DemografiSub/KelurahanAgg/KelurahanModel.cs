using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;

namespace Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;

public class KelurahanModel: IKelurahanKey
{
    public KelurahanModel(string id, string name, string kodePos)
    {
        KelurahanId = id;
        KelurahanName = name;
        KodePos = kodePos;
        KecamatanId = string.Empty;
        KecamatanName = string.Empty;
        KabupatenId = string.Empty;
        KabupatenName = string.Empty;
        PropinsiId = string.Empty;
        PropinsiName = string.Empty;
    }

    public static KelurahanModel Create(string id, string name, string kodePos) 
        => new KelurahanModel(id, name, kodePos);

    public void Set(KecamatanModel kecamatan)
    {
        KecamatanId = kecamatan.KecamatanId;
        KecamatanName = kecamatan.KecamatanName;
        KabupatenId = kecamatan.KabupatenId;
        KabupatenName = kecamatan.KabupatenName;
        PropinsiId = kecamatan.PropinsiId;
        PropinsiName = kecamatan.PropinsiName;
    }
    
    public string KelurahanId { get; private set; }
    public string KelurahanName { get; private set; }
    public string KecamatanId { get; private set; }
    public string KecamatanName { get; private set; }
    public string KabupatenId { get; private set; }
    public string KabupatenName { get; private set; }
    public string PropinsiId { get; private set; }
    public string PropinsiName { get; private set; }
    public string KodePos { get; private set; }
}

public interface IKelurahanKey
{
    string KelurahanId { get; }
}