namespace Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;

public class TipeTarifModel(string id, string name) : ITipeTarifKey
{
    public string TipeTarifId { get; protected set; } = id;
    public string TipeTarifName { get; protected set; } = name;
    public bool IsAktif { get; protected set; } = false;
    public decimal NoUrut { get; protected set; } = 0;
    
    // METHOD
    public void Activate() => IsAktif = true;
    public void Deactivate() => IsAktif = false;
    
    public void SetNoUrut(Decimal noUrut) => NoUrut = noUrut;
}


