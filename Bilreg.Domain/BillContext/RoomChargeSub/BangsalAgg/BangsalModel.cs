using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;

public class BangsalModel (string id, string name) : IBangsalKey
{
    public string BangsalId { get; protected set; } = id;
    public string BangsalName { get; protected set; } = name;
    public string LayananId { get; protected set; }
    public string LayananName { get; protected set; }
    public void SetLayanan(LayananModel layanan)
    {
        Guard.IsNotNull(layanan);
        
        LayananId = layanan.LayananId;
        LayananName = layanan.LayananName;
    }
}

public interface IBangsalKey
{
    string BangsalId { get; }
}