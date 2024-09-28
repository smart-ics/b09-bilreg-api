using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KamarAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.BillContext.RoomChargeSub.BedAgg;

public class BedModel(string id, string name) : IBedKey
{
    public string BedId { get; protected set; } = id;
    public string BedName { get; protected set; } = name;
    public string Keterangan { get; protected set; }
    public bool IsAktif { get; protected set; } = true;
    
    public string KamarId { get; protected set; }
    public string KamarName { get; protected set; }
    
    public string BangsalId { get; protected set; } = string.Empty;
    public string BangsalName { get; protected set; } = string.Empty;
    
    // METHOD
    public void SetKeterangan(string keterangan) => Keterangan = keterangan;
    public void SetAktif() => IsAktif = true;
    public void UnSetAktif() => IsAktif = false;

    public void SetKamar(KamarModel kamar)
    {
        Guard.IsNotNull(kamar);
        KamarId = kamar.KamarId;
        KamarName = kamar.KamarName;
    }

    public void SetBangsal(BangsalModel bangsal)
    {
        Guard.IsNotNull(bangsal);
        BangsalId = bangsal.BangsalId;
        BangsalName = bangsal.BangsalName;
    }
}
