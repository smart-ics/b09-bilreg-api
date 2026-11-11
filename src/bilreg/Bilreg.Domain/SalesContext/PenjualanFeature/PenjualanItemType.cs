using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.SalesContext.ResepFeature;

namespace Bilreg.Domain.SalesContext.PenjualanFeature;

[Obsolete("Deprecated in favor of Bilreg.Domain.ApotekContext.PenjualanFeature")]
public record PenjualanItemType
{
    private readonly List<PenjualanItemRacikType> _listItemRacik;
    private readonly List<PenjualanItemBatchType> _listBatch;

    public PenjualanItemType(
        string penjualanItemId,
        int noUrut,
        BrgReff brg,
        SatuanType satuan,
        decimal qty,
        EtiketType etiket,
        NilaiItemType nilai,
        bool isVoided,
        IEnumerable<PenjualanItemRacikType> listItemRacik,
        IEnumerable<PenjualanItemBatchType>? listBatch = null)
    {
        PenjualanItemId = penjualanItemId;
        NoUrut = noUrut;
        Brg = brg;
        Satuan = satuan;
        Qty = qty;
        Etiket = etiket;
        Nilai = nilai;
        IsVoided = isVoided;
        _listItemRacik = listItemRacik.ToList();
        _listBatch = listBatch?.ToList() ?? [];
    }

    public string PenjualanItemId { get; init; }
    public int NoUrut { get; private set; }
    public BrgReff Brg { get; init; }
    public SatuanType Satuan { get; init; }
    public decimal Qty { get; private set; }
    public EtiketType Etiket { get; init; }
    public NilaiItemType Nilai { get; private set; }
    public bool IsVoided { get; private set; }
    public IEnumerable<PenjualanItemRacikType> ListItemRacik => _listItemRacik;
    public IReadOnlyList<PenjualanItemBatchType> ListBatch => _listBatch;

    public static PenjualanItemType Create(
        string penjualanItemId,
        int noUrut,
        IBrg brg,
        SatuanType satuan,
        decimal qty,
        EtiketType etiket,
        NilaiItemType nilai)
        => new(penjualanItemId, noUrut, brg.ToReff(), satuan, qty, etiket, nilai, false, [], []);

    public void SetNoUrut(int noUrut) => NoUrut = noUrut;

    public void ApplyNilai(NilaiItemType nilai) => Nilai = nilai;

    public void VoidLine()
    {
        IsVoided = true;
        foreach (var batch in _listBatch)
            batch.VoidBatch();
    }

    public void AddBatch(string noBatch, DateTime tglEd, string poId, string doId, decimal qty, decimal hpp)
    {
        var noUrut = _listBatch.Select(x => x.NoUrut).DefaultIfEmpty(0).Max() + 1;
        var batchId = $"{PenjualanItemId}-{noUrut:D2}";
        _listBatch.Add(new PenjualanItemBatchType(batchId, noUrut, noBatch, tglEd, poId, doId, qty, hpp, IsVoided));
    }

    public void AddItemRacik(IBrg itemRacik, SatuanType satuan, decimal qty, decimal dosis, string dosisTxt)
    {
        var isItemDuplicated = _listItemRacik.Any(x => x.Brg.BrgId == itemRacik.BrgId);
        if (isItemDuplicated)
            throw new ArgumentException($"Item racik sudah ada, tidak bisa duplikasi.\n'{itemRacik}'");

        var noUrut = _listItemRacik
            .Select(x => x.NoUrut)
            .DefaultIfEmpty(0)
            .Max() + 1;

        _listItemRacik.Add(new PenjualanItemRacikType(noUrut, itemRacik.ToReff(), satuan, qty, dosis, dosisTxt));
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
