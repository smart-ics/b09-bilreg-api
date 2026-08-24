using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.SalesContext.ReturJualFeature;

public class ReturJualModel : IReturJualKey
{
    private readonly List<ReturJualItemType> _listItem;

    #region CREATE
    public ReturJualModel(
        string returJualId,
        PenjualanReff penjualan,
        LayananReff layanan,
        string reason,
        TipeJaminanReff tipeJaminan,
        NilaiReturJualType nilai,
        AuditTrailType auditTrail,
        IEnumerable<ReturJualItemType> listItem)
    {
        ReturJualId = returJualId;
        Penjualan = penjualan;
        Layanan = layanan;
        Reason = reason;
        TipeJaminan = tipeJaminan;
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
        NilaiReturJualType.Default,
        AuditTrailType.Default,
        []);

    public static ReturJualModel Key(string id) => new(
        id,
        PenjualanModel.Default.ToReff(),
        LayananType.Default.ToReff(),
        string.Empty,
        new TipeJaminanReff("-", "-"),
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
            new TipeJaminanReff(penjualan.TipeJaminan.TipeJaminanId, penjualan.TipeJaminan.TipeJaminanName),
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
    public TipeJaminanReff TipeJaminan { get; init; }
    public NilaiReturJualType Nilai { get; private set; }
    public AuditTrailType AuditTrail { get; private set; }
    public IEnumerable<ReturJualItemType> ListItem => _listItem;
    #endregion

    #region BEHAVIOR
    public void AddItem(ReturableItemType item, decimal qtyRetur, NilaiItemReturType nilai)
    {
        EnsureNotVoided();

        if (qtyRetur <= 0)
            throw new ArgumentException("Qty retur harus lebih besar dari 0.");

        if (qtyRetur > item.QtySisaRetur)
            throw new ArgumentException($"Qty retur tidak boleh melebihi sisa qty retur. " + $"Sisa: {item.QtySisaRetur}.");

        if (_listItem.Any(x => x.Brg.BrgId == item.Brg.BrgId && !x.IsVoided))
            throw new ArgumentException($"Brg sudah ada, tidak bisa duplikasi.\n'{item.Brg.BrgId}'");

        var noUrut = _listItem.Select(x => x.NoUrut).DefaultIfEmpty(0).Max() + 1;
        var itemId = $"{ReturJualId}{noUrut:D3}";
        _listItem.Add(ReturJualItemType.Create(
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

    public void ApplyNilaiItem(IBrg brg, NilaiItemReturType nilai)
    {
        EnsureNotVoided();
        var item = _listItem.FirstOrDefault(x => x.Brg.BrgId == brg.BrgId && !x.IsVoided) 
            ?? throw new KeyNotFoundException($"Item retur jual tidak ditemukan.\n'{brg}'");

        item.ApplyNilai(nilai);
        Recalculate();
    }

    public void SetPembulatan(decimal pembulatan = 0)
    {
        EnsureNotVoided();
        Nilai = NilaiReturJualType.RecalcFrom(_listItem, pembulatan);
    }

    public void Modify(string userId)
    {
        EnsureNotVoided();
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void Void(string userId)
    {
        AuditTrail.Batal(userId, DateTime.Now);
        foreach (var item in _listItem.Where(x => !x.IsVoided))
            item.VoidLine();
        Recalculate();
    }

    public ReturJualReff ToReff() => new(ReturJualId, AuditTrail.Created.Timestamp, Penjualan);

    public void Recalculate()
    {
        Nilai = NilaiReturJualType.RecalcFrom(_listItem, Nilai.Pembulatan);
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

    #endregion
}

public record ReturableItemType(BrgReff Brg, SatuanType Satuan, decimal QtyJual, decimal QtySisaRetur);

public record ReturJualReff(string ReturJualId, DateTime ReturJualDate, PenjualanReff Penjualan);