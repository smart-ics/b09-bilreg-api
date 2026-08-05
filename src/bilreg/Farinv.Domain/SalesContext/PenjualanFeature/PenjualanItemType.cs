using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.SalesContext.ResepFeature;

namespace Farinv.Domain.SalesContext.PenjualanFeature;

public record PenjualanItemType
{
    private readonly List<PenjualanItemRacikType> _listItemRacik;

    public PenjualanItemType(int noUrut, BrgReff brg, SatuanType satuan, EtiketType etiket, decimal qty, decimal harga,
        decimal diskon, decimal embalase, decimal tax, decimal total, decimal hpp, DateTime expiredDate, string batch,
        IEnumerable<PenjualanItemRacikType> listItemRacik)
    {
        NoUrut = noUrut;
        Brg = brg;
        Satuan = satuan;
        Etiket = etiket;
        Qty = qty;
        Harga = harga;
        Diskon = diskon;
        Embalase = embalase;
        Tax = tax;
        Total = total;
        Hpp = hpp;
        ExpiredDate = expiredDate;
        Batch = batch;
        _listItemRacik = listItemRacik.ToList();
    }

    public int NoUrut { get; private set; }
    public BrgReff Brg { get; init; }
    public SatuanType Satuan { get; init; }
    public EtiketType Etiket { get; init; }
    public decimal Qty { get; init; }
    public decimal Harga { get; init; }
    public decimal Diskon { get; init; }
    public decimal Embalase { get; init; }
    public decimal Tax { get; init; }
    public decimal Total { get; init; }
    public decimal Hpp { get; set; }
    public DateTime ExpiredDate { get; set; }
    public string Batch { get; set; }
    public IEnumerable<PenjualanItemRacikType> ListItemRacik => _listItemRacik;
    
    public void SetNoUrut(int noUrut) => NoUrut = noUrut;
}