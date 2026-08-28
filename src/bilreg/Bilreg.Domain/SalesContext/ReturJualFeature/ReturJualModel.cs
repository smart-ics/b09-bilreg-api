using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.SalesContext.ReturJualFeature;

public class ReturJualModel : IReturJualKey
{
    private readonly List<ReturJualItemModel> _listItem;
    private int _rangePembulatan;

    #region CREATE
    public ReturJualModel(
        string returJualId,
        PenjualanReff penjualan,
        LayananReff layanan,
        string reason,
        TipeJaminanReff tipeJaminan,
        TipeBrgReff tipeBrg,
        NilaiReturJualType nilai,
        AuditTrailType auditTrail,
        IEnumerable<ReturJualItemModel> listItem)
    {
        ReturJualId = returJualId;
        Penjualan = penjualan;
        Layanan = layanan;
        Reason = reason;
        TipeJaminan = tipeJaminan;
        TipeBrg = tipeBrg;
        Nilai = nilai;
        AuditTrail = auditTrail;
        _listItem = listItem.ToList();
    }

    public static ReturJualModel Default => new(
        "-",
        PenjualanModel.Default.ToReff(),
        LayananType.Default.ToReff(),
        string.Empty,
        new TipeJaminanReff("-", "-"),
        new TipeBrgReff("-", "-"),
        NilaiReturJualType.Default,
        AuditTrailType.Default,
        []);

    public static ReturJualModel Key(string id) => new(
        id,
        PenjualanModel.Default.ToReff(),
        LayananType.Default.ToReff(),
        string.Empty,
        new TipeJaminanReff("-", "-"),
        new TipeBrgReff("-", "-"),
        NilaiReturJualType.Default,
        AuditTrailType.Default,
        []);

    public static ReturJualModel Create(
        PenjualanModel penjualan, LayananType layanan, string reason, string userId)
    {
        var newId = NunaId.NewLegacy("RU", 'A');         

        return new ReturJualModel(
            newId,
            penjualan.ToReff(),
            layanan.ToReff(),
            reason,
            penjualan.TipeJaminan,
            penjualan.TipeBrg,
            NilaiReturJualType.Default,
            AuditTrailType.Create(userId, DateTime.Now),
            []);
    }
    #endregion

    #region PROPERTIES
    public string ReturJualId { get; private set; }
    public PenjualanReff Penjualan { get; private set; }
    public LayananReff Layanan { get; private set; }
    public string Reason { get; private set; }
    public TipeJaminanReff TipeJaminan { get; private set; }
    public TipeBrgReff TipeBrg { get; private set; }
    public NilaiReturJualType Nilai { get; private set; }
    public AuditTrailType AuditTrail { get; private set; }
    public IReadOnlyList<ReturJualItemModel> ListItem => _listItem;
    #endregion

    #region BEHAVIOR
    public void AddItem(ReturableItemType item, decimal qtyRetur, decimal hargaRetur, decimal tax)
    {
        EnsureNotVoided();

        if (qtyRetur <= 0)
            throw new ArgumentException("Qty retur harus lebih besar dari 0.");

        if (qtyRetur > item.QtySisaRetur)
            throw new ArgumentException($"Qty retur tidak boleh melebihi sisa qty retur. " + $"Sisa: {item.QtySisaRetur}.");

        if (_listItem.Any(x => x.Brg.BrgId == item.Brg.BrgId && !x.IsVoided))
            throw new ArgumentException($"Brg sudah ada, tidak bisa duplikasi.\n'{item.Brg.BrgId}'");

        var nilai = NilaiItemReturType.Create(
            item.QtyJual,
            qtyRetur,
            item.HargaJual,
            hargaRetur,
            tax);

        var noUrut = _listItem.Select(x => x.NoUrut).DefaultIfEmpty(0).Max() + 1;
        var itemId = $"{ReturJualId}{noUrut:D3}";
        _listItem.Add(ReturJualItemModel.Create(
            itemId, 
            noUrut, 
            item.Brg,
            item.Satuan,
            item.QtyJual,
            qtyRetur, 
            nilai));
        Recalculate();
    }

    public void RemoveItem(string returJualItemId)
    {
        EnsureNotVoided();
        var item = _listItem.FirstOrDefault(x => x.ReturJualItemId == returJualItemId) 
            ?? throw new KeyNotFoundException($"Item retur jual '{returJualItemId}' tidak ditemukan.");
        _listItem.Remove(item);
        RenumberItems();
        Recalculate();
    }

    public void ApplyNilaiItem(string returJualItemId, decimal hargaRetur, decimal taxPerUnit)
    {
        EnsureNotVoided();
        var item = GetItem(returJualItemId);
        item.ApplyNilai(hargaRetur, taxPerUnit);
        Recalculate();
    }

    public void ChangeQtyRetur(string returJualItemId, decimal qtyRetur, decimal qtySisaRetur)
    {
        EnsureNotVoided();

        if (qtyRetur <= 0)
            throw new ArgumentException("Qty retur harus lebih besar dari 0.");
        if (qtyRetur > qtySisaRetur)
            throw new ArgumentException($"Qty retur tidak boleh melebihi sisa qty retur. Sisa: {qtySisaRetur}.");

        var item = GetItem(returJualItemId);
        item.ChangeQtyRetur(qtyRetur);

        Recalculate();
    }

    public void SetPembulatan(int rangePembulatan)
    {
        EnsureNotVoided();
        if (rangePembulatan < 0)
            throw new ArgumentException("Range pembulatan tidak boleh kurang dari 0.");

        _rangePembulatan = rangePembulatan;
        Recalculate();
    }

    public void Modify(string userId)
    {
        EnsureNotVoided();
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void Void(string userId)
    {
        EnsureNotVoided();
        AuditTrail.Batal(userId, DateTime.Now);
        foreach (var item in _listItem.Where(x => !x.IsVoided))
            item.VoidLine();
        Recalculate();
    }

    public ReturJualReff ToReff() => new(ReturJualId, AuditTrail.Created.Timestamp, Penjualan);

    private void Recalculate()
    {
        Nilai = NilaiReturJualType.RecalcFrom(_listItem, _rangePembulatan);
    }

    private void RenumberItems()
    {
        var i = 1;
        foreach (var item in _listItem)
        {
            item.SetNoUrut(i);
            i++;
        }
    }

    private void EnsureNotVoided()
    {
        if (AuditTrail.IsVoided)
            throw new InvalidOperationException("Retur Jual sudah void, tidak bisa diubah.");
    }

    private ReturJualItemModel GetItem(string returJualItemId)
        => _listItem.FirstOrDefault(x => x.ReturJualItemId == returJualItemId && !x.IsVoided)
            ?? throw new KeyNotFoundException($"Item retur jual '{returJualItemId}' tidak ditemukan.");
    #endregion
}

public record ReturableItemType(BrgReff Brg, SatuanType Satuan, decimal QtyJual, decimal HargaJual, decimal QtySisaRetur);

public record ReturJualReff(string ReturJualId, DateTime ReturJualDate, PenjualanReff Penjualan);