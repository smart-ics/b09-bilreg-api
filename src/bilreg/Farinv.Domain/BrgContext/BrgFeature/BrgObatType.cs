using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Domain.BrgContext.BrgFeature;

public record BrgObatType : IBrg, IKlasifikasiUmum, IPabrik, IFarmakologi
{
    private readonly List<BrgSatuanType> _listSatuan;
    #region CREATION
    //  identitas
    public BrgObatType(string brgId, string brgName, bool isAktif, string ketBarang, 
        GroupRekReff groupRek, GroupRekDkType groupRekDk,
        GolonganType golongan, GroupObatDkType groupObatDk, KelompokType kelompok, SifatType sifat, 
        BentukType bentuk, PabrikType pabrik, 
        GenerikReff generik, KelasTerapiType kelasTerapi, GolTerapiType golTerapi, 
        OriginalType original, IEnumerable<BrgSatuanType> listSatuan)
    {
        BrgId = brgId;
        BrgName = brgName;
        IsAktif = isAktif;
        KetBarang = ketBarang;
        GroupRek = groupRek;
        GroupRekDk = groupRekDk;
        
        Golongan = golongan;
        GroupObatDk = groupObatDk;
        Kelompok = kelompok;
        Sifat = sifat;
        Bentuk = bentuk;
        Pabrik = pabrik;
        
        Generik = generik;
        KelasTerapi = kelasTerapi;
        GolTerapi = golTerapi;
        Original = original;
        
        _listSatuan = listSatuan?.ToList() ?? [];
    }
    public static BrgObatType Default => new("-", "-", false, "-", 
        GroupRekType.Default.ToReff(), GroupRekDkType.Default, 
        GolonganType.Default,  GroupObatDkType.Default, KelompokType.Default, SifatType.Default, 
        BentukType.Default, PabrikType.Default, 
        GenerikType.Default.ToReff(), KelasTerapiType.Default, GolTerapiType.Default, 
        OriginalType.Default, []);
    public static IBrgKey Key(string id) => Default with { BrgId = id };
    #endregion
    
    #region PROPERTIES
    public string BrgId { get; init; }
    public string BrgName { get; init; }
    public bool IsAktif { get; init; }
    public string KetBarang { get; init; }
    public GroupRekReff GroupRek { get; init; }
    public GroupRekDkType GroupRekDk { get; init; }
    //  klasifikasi
    public GolonganType Golongan { get; init; }
    public GroupObatDkType GroupObatDk { get; init; }
    public KelompokType Kelompok { get; init; }
    public SifatType Sifat { get; init; }
    public BentukType Bentuk { get; init; }
    public PabrikType Pabrik { get; init; }
    //  farmakologi
    public GenerikReff Generik { get; init; }
    public KelasTerapiType KelasTerapi { get; init; }
    public GolTerapiType GolTerapi { get; init; }
    public OriginalType Original { get; init; }


    public IEnumerable<BrgSatuanType> ListSatuan => _listSatuan;
    #endregion
    
    #region BEHAVIOUR
    public BrgReff ToReff()
    {
        return new BrgReff(BrgId, BrgName);
    }
    #endregion
}