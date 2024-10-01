namespace Bilreg.Domain.BillContext.RoomChargeSub.TipeKamarAgg;

public class TipeKamarModel(string TipeKamarId, string TipeKamarName) : ITipeKamarKey
{
    public string TipeKamarId { get; protected set; } = TipeKamarId;
    public string TipeKamarName { get; protected set; } = TipeKamarName;
    public bool IsGabung { get; protected set; } = false;
    public bool IsAktif { get; protected set; } = false;
    public bool IsDefault { get; protected set; }
    public int NoUrut { get; protected set; } = 0;

    public void SetGabung() => IsGabung = true;
    public void SetAktif() => IsAktif = true;
    public void UnSetAktif() => IsAktif = false;

    public void SetDefault() => IsDefault = true;
    public void ResetDefault() => IsDefault = false;
    public void SetNoUrut(int noUrut) => NoUrut = noUrut;
    
}

public interface ITipeKamarKey
{
    string TipeKamarId { get;}
}