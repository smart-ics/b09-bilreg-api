using Ardalis.GuardClauses;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Domain.Shared.Helpers.CommonValueObjects;

namespace Farinv.Domain.SalesContext.ResepFeature;

public record DokterType: IDokterKey
{
    private readonly List<LayananReff> _listLayanan;
    
    public DokterType(string dokterId, string dokterName, IEnumerable<LayananReff> listLayanan)
    {
        Guard.Against.NullOrWhiteSpace(dokterId);
        Guard.Against.NullOrWhiteSpace(dokterName);
        
        DokterId = dokterId;
        DokterName = dokterName;
        _listLayanan = listLayanan.ToList();
    }
    
    public static DokterType Default => new DokterType(AppConst.DASH, AppConst.DASH, new List<LayananReff>());
    
    public static DokterType Key(string id) => Default with { DokterId = id };
    
    public DokterReff ToReff() => new(DokterId, DokterName);
    
    public string DokterId { get; init; }
    public string DokterName { get; init; }
    public IEnumerable<LayananReff> ListLayanan => _listLayanan;
}

public record DokterReff(string DokterId, string DokterName): IDokterKey;