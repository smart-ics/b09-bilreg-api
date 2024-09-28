using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.BillContext.RoomChargeSub.KamarAgg;

public class KamarModel(
    string KamarId,
    string KamarName
): IKamarKey
{
    public string KamarId { get; protected set; } = KamarId;
    public string KamarName { get; protected set; } = KamarName;
    
    public string Ket1 { get; protected set; } = string.Empty;
    public string Ket2 { get; protected set; } = string.Empty;
    public string Ket3 { get; protected set; } = string.Empty;
    
    public decimal JumlahKamar { get; protected set; }
    public decimal JumlahKamarPakai { get; protected set; }
    public decimal JumlahKamarKotor { get; protected set; }
    public decimal JumlahKamarRusak { get; protected set; }
    
    public string BangsalId { get; protected set; } = string.Empty;
    public string BangsalName { get; protected set; } = string.Empty;
    
    public string KelasId { get;protected set; } = string.Empty;
    public string KelasName { get; protected set; } = string.Empty;

    public void SetBangsal(BangsalModel bangsal)
    {;
        BangsalId = bangsal.BangsalId;
        BangsalName = bangsal.BangsalName;
    }
    public void SetKelas(KelasModel kelas)
    {
        KelasId = kelas.KelasId;
        KelasName = kelas.KelasName;

    }

    public void SetKet(string ket1,string ket2,string ket3)
    {
        Ket1 = ket1;
        Ket2 = ket2;
        Ket3 = ket3;
    }
}

public interface IKamarKey
{
    string KamarId { get; }
}