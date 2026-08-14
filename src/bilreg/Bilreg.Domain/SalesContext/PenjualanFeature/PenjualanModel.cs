using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;
using TipeBrgType = Bilreg.Domain.BrgContext.PricingPolicyFeature.TipeBrgType;

namespace Bilreg.Domain.SalesContext.PenjualanFeature;

public class PenjualanModel : IPenjualanKey
{
    private readonly List<PenjualanItemType> _listItem;

    public PenjualanModel(
        string penjualanId,
        string resepId,
        RegReff reg,
        DokterReff dokter,
        LayananReff layanan,
        LayananReff layananResep,
        TipeJaminanReff tipeJaminan,
        TipeBrgReff tipeBrg,
        NilaiPenjualanType nilai,
        AuditTrailType auditTrail,
        IEnumerable<PenjualanItemType> listItem)
    {
        PenjualanId = penjualanId;
        ResepId = resepId;
        Register = reg;
        Dokter = dokter;
        Layanan = layanan;
        LayananResep = layananResep;
        TipeJaminan = tipeJaminan;
        TipeBrg = tipeBrg;
        Nilai = nilai;
        AuditTrail = auditTrail;
        _listItem = listItem.ToList();
    }

    public static PenjualanModel Key(string id) => new(
        id,
        AppConst.DASH,
        RegModel.Default.ToReff(),
        DokterType.Default.ToReff(),
        LayananType.Default.ToReff(),
        LayananType.Default.ToReff(),
        new TipeJaminanReff(AppConst.DASH, AppConst.DASH),
        TipeBrgType.Default.ToReff(),
        NilaiPenjualanType.Default,
        AuditTrailType.Default,
        []);

    public static PenjualanModel CreateFromResep(
        ResepModel resep,
        TipeJaminanReff tipeJaminan,
        LayananType layananJual,
        string userId)
    {
        var newId = NunaId.NewLegacy("DU", 'A');
        var listItem = new List<PenjualanItemType>();
        var noUrut = 0;

        foreach (var obat in resep.ListObat)
        {
            noUrut++;
            var itemId = $"{newId}{noUrut:D3}";
            var racikItems = obat.ListItemRacik
                .Select(r => new PenjualanItemRacikType(r.NoUrut, r.Brg, r.Satuan, r.Qty, r.Dosis, r.DosisTxt));

            listItem.Add(new PenjualanItemType(
                itemId,
                noUrut,
                obat.Brg,
                obat.Satuan,
                obat.Qty,
                obat.Etiket,
                NilaiItemType.Default,
                false,
                racikItems));
        }

        return new PenjualanModel(
            newId,
            resep.ResepId,
            resep.Register,
            resep.Dokter,
            layananJual.ToReff(),
            resep.Layanan,
            tipeJaminan,
            resep.TipeBrg,
            NilaiPenjualanType.Default,
            AuditTrailType.Create(userId, DateTime.Now),
            listItem);
    }

    public string PenjualanId { get; private set; }
    public string ResepId { get; private set; }
    public RegReff Register { get; private set; }
    public DokterReff Dokter { get; private set; }
    public LayananReff Layanan { get; private set; }
    public LayananReff LayananResep { get; private set; }
    public TipeJaminanReff TipeJaminan { get; private set; }
    public TipeBrgReff TipeBrg { get; private set; }
    public NilaiPenjualanType Nilai { get; private set; }
    public AuditTrailType AuditTrail { get; private set; }
    public IEnumerable<PenjualanItemType> ListItem => _listItem;

    public void AddItem(IBrg brg, SatuanType satuan, decimal qty, EtiketType etiket, NilaiItemType nilai)
    {
        EnsureNotVoided();

        if (_listItem.Any(x => x.Brg.BrgId == brg.BrgId && !x.IsVoided))
            throw new ArgumentException($"Brg sudah ada, tidak bisa duplikasi.\n'{brg}'");

        var noUrut = _listItem.Select(x => x.NoUrut).DefaultIfEmpty(0).Max() + 1;
        var itemId = $"{PenjualanId}{noUrut:D3}";
        _listItem.Add(PenjualanItemType.Create(itemId, noUrut, brg, satuan, qty, etiket, nilai));
        Recalculate();
    }

    public void RemoveItem(IBrg brg)
    {
        EnsureNotVoided();
        _listItem.RemoveAll(x => x.Brg.BrgId == brg.BrgId);
        RenumberItems();
        Recalculate();
    }

    public void AddItemRacik(IBrg obatRacik, IBrg itemRacik, SatuanType satuan, decimal qty, decimal dosis, string dosisTxt)
    {
        EnsureNotVoided();
        var item = _listItem.FirstOrDefault(x => x.Brg.BrgId == obatRacik.BrgId && !x.IsVoided);
        if (item is null)
            throw new KeyNotFoundException($"Obat racik tidak ditemukan.\n'{obatRacik}'");
        item.AddItemRacik(itemRacik, satuan, qty, dosis, dosisTxt);
    }

    public void RemoveItemRacik(IBrg obatRacik, IBrg itemRacik)
    {
        EnsureNotVoided();
        var item = _listItem.FirstOrDefault(x => x.Brg.BrgId == obatRacik.BrgId);
        if (item is null)
            return;

        item.RemoveItemRacik(itemRacik);
        if (!item.ListItemRacik.Any())
            _listItem.RemoveAll(x => x.Brg.BrgId == obatRacik.BrgId);

        RenumberItems();
        Recalculate();
    }

    public void ApplyNilaiItem(IBrg brg, NilaiItemType nilai)
    {
        EnsureNotVoided();
        var item = _listItem.FirstOrDefault(x => x.Brg.BrgId == brg.BrgId && !x.IsVoided);
        if (item is null)
            throw new KeyNotFoundException($"Item penjualan tidak ditemukan.\n'{brg}'");

        item.ApplyNilai(nilai);
        Recalculate();
    }

    public void SetHeaderAdjustment(decimal diskonLain, decimal biayaLain, decimal pembulatan = 0, decimal bulat = 0)
    {
        EnsureNotVoided();
        Nilai = NilaiPenjualanType.RecalcFrom(_listItem, diskonLain, biayaLain, pembulatan, bulat);
    }

    public void Recalculate()
    {
        Nilai = NilaiPenjualanType.RecalcFrom(
            _listItem,
            Nilai.DiskonLain,
            Nilai.BiayaLain,
            Nilai.Pembulatan,
            Nilai.Bulat);
    }

    public void Modify(string userId) => AuditTrail.Modif(userId, DateTime.Now);

    public void Void(string userId)
    {
        AuditTrail.Batal(userId, DateTime.Now);
        foreach (var item in _listItem.Where(x => !x.IsVoided))
            item.VoidLine();
        Recalculate();
    }

    public PenjualanReff ToReff() => new(PenjualanId, AuditTrail.Created.Timestamp, Register);

    private void EnsureNotVoided()
    {
        if (AuditTrail.IsVoided)
            throw new InvalidOperationException("Penjualan sudah void, tidak bisa diubah.");
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
}

public record PenjualanReff(string PenjualanId, DateTime PenjualanDate, RegReff Reg);
