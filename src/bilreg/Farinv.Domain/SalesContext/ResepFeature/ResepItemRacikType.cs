using Farinv.Domain.BrgContext.BrgFeature;

namespace Farinv.Domain.SalesContext.ResepFeature;

public record ResepItemRacikType
{
    public ResepItemRacikType(int noUrut, BrgReff brg, SatuanType satuan, decimal qty, decimal dosis, string dosisTxt)
    {
        NoUrut = noUrut;
        Brg = brg;
        Satuan = satuan;
        Qty = qty;
        Dosis = dosis;
        DosisTxt = dosisTxt;
    }
    
    public int NoUrut { get; private set; }
    public BrgReff Brg { get; init; }
    public SatuanType Satuan { get; init; }
    public decimal Qty { get; init; }
    public decimal Dosis { get; init; }
    public string DosisTxt { get; init; }

    public void SetNoUrut(int noUrut) => NoUrut = noUrut;
}