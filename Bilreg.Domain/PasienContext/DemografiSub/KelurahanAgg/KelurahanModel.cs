using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;

public class KelurahanModel: IKelurahanKey
{
    public KelurahanModel(string id, string name, string kodePos, KecamatanModel kecamatan)
    {
        Guard.IsNotNullOrEmpty(id);
        Guard.IsNotNullOrEmpty(name);
        Guard.IsNotNull(kecamatan);
        
        KelurahanId = id;
        KelurahanName = name;
        KodePos = kodePos;
        Kecamatan = kecamatan;
    }

    public string KelurahanId { get; private set; }
    public string KelurahanName { get; private set; }
    public KecamatanModel Kecamatan { get; private set; }
    public string KodePos { get; private set; }
}

public interface IKelurahanKey
{
    string KelurahanId { get; }
}