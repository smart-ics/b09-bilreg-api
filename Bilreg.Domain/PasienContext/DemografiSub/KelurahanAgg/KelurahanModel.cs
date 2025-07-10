using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using CommunityToolkit.Diagnostics;
using Xunit;

namespace Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;

public class KelurahanModel: IKelurahanKey
{
    public KelurahanModel(string id, string name, 
        string kodePos, KecamatanModel kecamatan)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException("Invalid Kelurahan");
        
        Guard.IsNotNull(kecamatan);
        
        KelurahanId = id;
        KelurahanName = name;
        KodePos = kodePos;
        Kecamatan = kecamatan;
    }

    public static KelurahanModel Default => 
        new KelurahanModel(string.Empty, string.Empty, 
            string.Empty, KecamatanModel.Default);
    public string KelurahanId { get; private set; }
    public string KelurahanName { get; private set; }
    public KecamatanModel Kecamatan { get; private set; }
    public string KodePos { get; private set; }
    public KelurahanViewType ToViewType() 
        => new(KelurahanId, KelurahanName, 
            Kecamatan.KecamatanName, Kecamatan.Kabupaten.KabupatenName, 
            Kecamatan.Kabupaten.Propinsi.PropinsiName);
}