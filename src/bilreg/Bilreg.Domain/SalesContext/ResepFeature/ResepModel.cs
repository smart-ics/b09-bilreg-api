using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.SalesContext.ResepFeature;

public class ResepModel : IResepKey
{
    private readonly List<ResepObatType> _listObat;

    public ResepModel(string resepId, RegReff reg, BodyMetricType bodyMetric, DokterReff dokter, LayananReff layanan,
        UrgenitasType urgenitas, TipeBrgReff tipeBrg, int iter, string description, AuditTrailType auditTrail,
        IEnumerable<ResepObatType> listObat)
    {
        ResepId = resepId;
        Register = reg;
        BodyMetric = bodyMetric;
        Dokter = dokter;
        Layanan = layanan;
        Urgenitas = urgenitas;
        TipeBrg = tipeBrg;
        Iter = iter;
        Description = description;
        AuditTrail = auditTrail;
        _listObat = listObat.ToList();
    }

    public static ResepModel Key(string id) => new ResepModel(id, RegModel.Default.ToReff(), BodyMetricType.Default(),
        DokterType.Default.ToReff(), LayananType.Default.ToReff(), UrgenitasType.Default, TipeBrgType.Default.ToReff(), 
        0, AppConst.DASH, AuditTrailType.Default, new List<ResepObatType>());

    public static ResepModel Create(RegModel reg, BodyMetricType bodyMetric, DokterType dokter, LayananType layanan, 
        UrgenitasType urgenitasType, TipeBrgType tipeBrg, int iter, string description, string userId)
    {
        var newId = NunaId.NewLegacy("KP",'A');
        var model = new ResepModel(newId, reg.ToReff(), bodyMetric, dokter.ToReff(), layanan.ToReff(),
            urgenitasType, tipeBrg.ToReff(), iter, description, AuditTrailType.Create(userId, DateTime.Now),
            new List<ResepObatType>());
        return model;
    }

    public string ResepId { get; private set; }
    public RegReff Register { get; private set; }
    public BodyMetricType BodyMetric { get; private set; }

    public DokterReff Dokter { get; private set; }
    public LayananReff Layanan { get; private set; }

    public UrgenitasType Urgenitas { get; private set; }
    public TipeBrgReff TipeBrg { get; private set; }

    public int Iter { get; private set; }
    public string Description { get; private set; }

    public AuditTrailType AuditTrail { get; private set; }
    public IEnumerable<ResepObatType> ListObat => _listObat;
    
    public void AddObat(IBrg brg, SatuanType satuan, decimal qty, int iter, string signa, string instruction, string note)
    {
        var isBrgDuplicated = _listObat
            .Any(x => x.Brg.BrgId == brg.BrgId);
        
        if (isBrgDuplicated)
            throw new ArgumentException($"Brg sudah ada, tidak bisa duplikasi.\n'{brg}");
        
        var noUrut = ListObat
            .Select(x => x.NoUrut)
            .DefaultIfEmpty(0)
            .Max() + 1;
        
        var newObat = ResepObatType.Create(noUrut, brg, satuan, qty, iter, signa, instruction, note);
        _listObat.Add(newObat);
    }

    public void RemoveObat(IBrg brg)
    {
        _listObat.RemoveAll(x => x.Brg.BrgId == brg.BrgId);
        var i = 1;
        foreach (var item in _listObat)
        {
            item.SetNoUrut(i);
            i++;
        }
    }
    
    public void AddItemRacik(IBrg obatRacik, IBrg itemRacik, SatuanType satuan, decimal qty, decimal dosis, string dosisTxt)
    {
        var obat = _listObat.FirstOrDefault(x => x.Brg.BrgId == obatRacik.BrgId);
        if (obat is null)
            throw new KeyNotFoundException($"$Obat Racik tidak ditemukan.\n'{obatRacik}'");
        obat.AddItemRacik(itemRacik, satuan, qty, dosis, dosisTxt);
    }
    
    public void RemoveItemRacik(IBrg obatRacik, IBrg itemRacik)
    {
        var obat = _listObat.FirstOrDefault(x => x.Brg.BrgId == obatRacik.BrgId);
        if (obat is null)
            return;
        
        obat.RemoveItemRacik(itemRacik);
        if (!obat.ListItemRacik.Any())
            _listObat.RemoveAll(x => x.Brg.BrgId == obatRacik.BrgId);
    }
    
    public void Modify(string userId) => AuditTrail.Modif(userId, DateTime.Now);
    public void Void(string userId) => AuditTrail.Batal(userId, DateTime.Now);
}