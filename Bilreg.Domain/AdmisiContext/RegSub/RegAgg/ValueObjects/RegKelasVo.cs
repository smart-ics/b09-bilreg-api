using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

public record RegKelasVo
{
    public string KelasId { get; }
    public string KelasName { get; }
    
    public RegKelasVo(string kelasId, string kelasName)
    {
        Guard.IsNotNullOrEmpty(kelasId);
        Guard.IsNotNullOrEmpty(kelasName);
        
        KelasId = kelasId;
        KelasName = kelasName;
    }

    public RegKelasVo(KelasModel kelas)
    {
        Guard.IsNotNull(kelas);
        Guard.IsNotNullOrEmpty(kelas.KelasId);
        Guard.IsNotNullOrEmpty(kelas.KelasName);

        KelasId = kelas.KelasId;
        KelasName = kelas.KelasName;
    }
}