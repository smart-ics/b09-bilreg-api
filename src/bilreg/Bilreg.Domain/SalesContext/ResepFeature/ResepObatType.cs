using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.SalesContext.ResepFeature;

public record ResepObatType
{
    private readonly List<ResepItemRacikType> _listItemRacik;
    private readonly bool _isRacik;

    public ResepObatType(int noUrut, BrgReff brg, SatuanType satuan, decimal qty, int iter, EtiketType etiket, bool isRacik = false)
    {
        if (iter < 0)
            throw new ArgumentException("Iter tidak boleh bernilai negatif.", nameof(iter));

        NoUrut = noUrut;
        Brg = brg;
        Satuan = satuan;
        Qty = qty;
        Iter = iter;
        Etiket = etiket;
        _isRacik = isRacik;
        _listItemRacik = [];
    }

    public ResepObatType(int noUrut, BrgReff brg, SatuanType satuan, decimal qty, int iter, EtiketType etiket, bool isRacik, IEnumerable<ResepItemRacikType> listItemRacik)
    {
        if (iter < 0)
            throw new ArgumentException("Iter tidak boleh bernilai negatif.", nameof(iter));

        NoUrut = noUrut;
        Brg = brg;
        Satuan = satuan;
        Qty = qty;
        Iter = iter;
        Etiket = etiket;
        _isRacik = isRacik;
        _listItemRacik = listItemRacik?.ToList() ?? [];
    }

    public int NoUrut { get; private set; }
    public BrgReff Brg { get; init; }
    public SatuanType Satuan { get; init; }
    public decimal Qty { get; init; }
    public int Iter { get; init; }
    public EtiketType Etiket { get; init; }
    public bool IsRacik => _isRacik || _listItemRacik.Any();
    public IEnumerable<ResepItemRacikType> ListItemRacik => _listItemRacik;

    public static ResepObatType Create(int noUrut, IBrg brg, SatuanType satuan, decimal qty, int iter, EtiketType etiket, bool isRacik = false)
        => new(noUrut, brg.ToReff(), satuan, qty, iter, etiket, isRacik);

    public static ResepObatType Create(int noUrut, IBrg brg, SatuanType satuan, decimal qty, int iter, string signa,
        string instruction, string note = AppConst.DASH, bool isRacik = false)
        => new(noUrut, brg.ToReff(), satuan, qty, iter, EtiketType.Create(signa, instruction, note), isRacik);

    public static ResepObatType CreateRacik(int noUrut, IBrg brg, SatuanType satuan, decimal qty, int iter, EtiketType etiket)
        => new(noUrut, brg.ToReff(), satuan, qty, iter, etiket, isRacik: true);

    public static ResepObatType CreateRacik(int noUrut, IBrg brg, SatuanType satuan, decimal qty, int iter, string signa,
        string instruction, string note = AppConst.DASH)
        => new(noUrut, brg.ToReff(), satuan, qty, iter, EtiketType.Create(signa, instruction, note), isRacik: true);

    public void SetNoUrut(int noUrut) => NoUrut = noUrut;

    public void AddItemRacik(IBrg itemRacik, SatuanType satuan, decimal qty, decimal dosis, string dosisTxt)
    {
        var isItemDuplicated = _listItemRacik.Any(x => x.Brg.BrgId == itemRacik.BrgId);
        if (isItemDuplicated)
            throw new ArgumentException($"Item racik sudah ada, tidak bisa duplikasi.\n'{itemRacik}'");

        var noUrut = _listItemRacik.Select(x => x.NoUrut).DefaultIfEmpty(0).Max() + 1;
        var newItem = new ResepItemRacikType(noUrut, itemRacik.ToReff(), satuan, qty, dosis, dosisTxt);
        _listItemRacik.Add(newItem);
    }

    public void AddItemRacik(ResepItemRacikType itemRacik)
    {
        var isItemDuplicated = _listItemRacik.Any(x => x.Brg.BrgId == itemRacik.Brg.BrgId);
        if (isItemDuplicated)
            throw new ArgumentException($"Item racik sudah ada, tidak bisa duplikasi.\n'{itemRacik.Brg}'");

        var noUrut = _listItemRacik.Select(x => x.NoUrut).DefaultIfEmpty(0).Max() + 1;
        itemRacik.SetNoUrut(noUrut);
        _listItemRacik.Add(itemRacik);
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

    public void ValidateRacik()
    {
        if (IsRacik && !_listItemRacik.Any())
            throw new InvalidOperationException($"Wadah racikan '{Brg.BrgName}' harus memiliki minimal 1 bahan komponen.");
    }
}