using Farinv.Domain.BrgContext.BrgFeature;

namespace Farinv.Domain.InventoryContext.StokFeature;

public class StokModel : IStokKey
{
    #region CREATION
    private readonly List<StokLayerModel> _listLayer;
    public StokModel(string brgId, string layananId, BrgReff brg, 
        LayananReff layanan, int qty, string satuan,
        IEnumerable<StokLayerModel> listLayer)
    {
        BrgId = brgId;
        LayananId = layananId;
        Brg = brg;
        Layanan = layanan;
        Qty = qty;
        Satuan = satuan;
        _listLayer = listLayer?.ToList() ?? [];
    }
    public static StokModel Default 
        => new StokModel("-", "-", BrgObatType.Default.ToReff(), 
        LayananType.Default.ToReff(), 0, "", []);
    public static IStokKey Key(string brgId, string layananId) 
        => new StokModel(brgId, layananId, BrgObatType.Default.ToReff(), 
        LayananType.Default.ToReff(), 0, "", []);

    public static StokModel Create(string brgId, string layananId,
        BrgReff brg, LayananReff layanan, string satuan)
    {
        return new StokModel(brgId, layananId, brg, layanan, 0, satuan, []);
    }
    #endregion
    
    #region PROPERTIES
    public string BrgId { get; }
    public string LayananId { get; }
    public BrgReff Brg { get; init; }
    public LayananReff Layanan { get; init; }
    public int Qty { get; private set; }
    public string Satuan { get; init; }
    public IEnumerable<StokLayerModel> ListLayer => _listLayer;
    #endregion

    public void AddStok(int qty, StokLotType stokLot, decimal hpp, 
        TrsReffType trsReff, string useCase)
    {
        var newLayer = StokLayerModel.Create(trsReff, stokLot, qty, 
            hpp, useCase);
        _listLayer.Add(newLayer);
        UpdateQty();
    }
    public void RemoveStok(int qty, TrsReffType trsReff, string useCase)
    {
        if (qty > Qty)
            throw new ArgumentException("Qty tidak boleh melebihi Qty Stok");

        var qtyToBeRemoved = qty;
        foreach (var item in _listLayer.OrderBy(x => x.StokLot.ExpDate))
        {
            var qtyRemove = Math.Min(qtyToBeRemoved, item.QtySisa);
            item.RemoveStok(qtyRemove, trsReff, useCase);
            qtyToBeRemoved -= qtyRemove;
            if (qtyToBeRemoved == 0)
                break;
        }
        if (qtyToBeRemoved > 0)
            throw new ArgumentException("Qty tidak boleh melebihi Qty Stok");
        
        UpdateQty();
    }
    
    private void UpdateQty() => Qty = _listLayer.Sum(x => x.QtySisa);
}

public interface IStokKey : IBrgKey, ILayananKey
{
}