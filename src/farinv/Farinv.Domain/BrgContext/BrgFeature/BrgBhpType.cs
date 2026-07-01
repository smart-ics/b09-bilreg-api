using Farinv.Domain.BrgContext.KlasifikasiFeature;

namespace Farinv.Domain.BrgContext.BrgFeature;

public record BrgBhpType : IBrg, IKlasifikasiUmum, IPabrik
{
    private readonly List<BrgSatuanType> _listSatuan;

    public BrgBhpType(string brgId, string brgName, bool isAktif, string ketBarang, 
        GroupRekReff groupRek, GroupRekDkType groupRekDk,
        GolonganType golongan, GroupObatDkType groupObatDk, 
        KelompokType kelompok, SifatType sifat, BentukType bentuk, 
        PabrikType pabrik, IEnumerable<BrgSatuanType> listSatuan)
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
        _listSatuan = listSatuan?.ToList() ?? [];
    }

    public static BrgBhpType Default => new("-", "-", false, "-",
        GroupRekType.Default.ToReff(), GroupRekDkType.Default, 
        GolonganType.Default, GroupObatDkType.Default, 
        KelompokType.Default, SifatType.Default,
        BentukType.Default, PabrikType.Default, []);
    public string BrgId { get; init;}
    public string BrgName { get; init;}
    public bool IsAktif { get; init;}
    public string KetBarang { get; init;}
    public GroupRekReff GroupRek { get; init;}
    public GroupRekDkType GroupRekDk { get; init; }

    public GolonganType Golongan { get; init;}
    public GroupObatDkType GroupObatDk { get; init;}
    public KelompokType Kelompok { get; init;}
    public SifatType Sifat { get; init;}
    public BentukType Bentuk { get; init;}
    public PabrikType Pabrik { get; init;}

    public IEnumerable<BrgSatuanType> ListSatuan => _listSatuan;
    public BrgReff ToReff() => new BrgReff(BrgId, BrgName);
}