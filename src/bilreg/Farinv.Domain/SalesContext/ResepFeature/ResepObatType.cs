using Farinv.Domain.BrgContext.BrgFeature;

namespace Farinv.Domain.SalesContext.ResepFeature;

public record ResepObatType
{
    private readonly List<ResepItemRacikType> _listItemRacik;

    public ResepObatType(int noUrut, BrgReff brg, SatuanType satuan, decimal qty, int iter, EtiketType etiket)
    {
        NoUrut = noUrut;
        Brg = brg;
        Satuan = satuan;
        Qty = qty;
        Iter = iter;
        Etiket = etiket;
        _listItemRacik = [];
    }

    public int NoUrut { get; private set; }
    public BrgReff Brg { get; init; }
    public SatuanType Satuan { get; init; }
    public decimal Qty { get; init; }
    public int Iter { get; init; }
    public EtiketType Etiket { get; init; }
    public IEnumerable<ResepItemRacikType> ListItemRacik => _listItemRacik;

    public static ResepObatType Create(int noUrut, IBrg brg, SatuanType satuan, decimal qty, int iter, string signa,
        string instruction, string note) => new(noUrut, brg.ToReff(), satuan, qty, iter,
        EtiketType.Create(signa, instruction, note));

    public void SetNoUrut(int noUrut) => NoUrut = noUrut;

    public void AddItemRacik(IBrg itemRacik, SatuanType satuan, decimal qty, decimal dosis, string dosisTxt)
    {
        var isItemDuplicated = _listItemRacik
            .Any(x => x.Brg.BrgId == itemRacik.BrgId);

        if (isItemDuplicated)
            throw new ArgumentException($"Item racik sudah ada, tidak bisa duplikasi.\n'{itemRacik}");

        var noUrut = _listItemRacik
            .Select(x => x.NoUrut)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var newItem = new ResepItemRacikType(noUrut, itemRacik.ToReff(), satuan, qty, dosis, dosisTxt);
        _listItemRacik.Add(newItem);
    }

    public void RemoveItemRacik(IBrg itemRacik)
    {
        _listItemRacik.RemoveAll(x => x.Brg.BrgId == itemRacik.BrgId);
        var i = 1;
        foreach (var item in _listItemRacik)
        {
            item.SetNoUrut(i);
            i++;
        }
    }
}