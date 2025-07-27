using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.RujukanSub;

public record RujukanType : IRujukanKey
{
    public RujukanType(string rujukanId, string rujukanName, bool isAktif,
        AlamatType alamat, TipeRujukanType tipeRujukan,
        KelasRujukanReff kelasRujukan, CaraMasukDkType caraMasukDk)
    {
        Guard.Against.NullOrWhiteSpace(rujukanId, nameof(rujukanId));
        Guard.Against.NullOrWhiteSpace(rujukanName, nameof(rujukanName));
        Guard.Against.Null(alamat, nameof(alamat));
        Guard.Against.Null(tipeRujukan, nameof(tipeRujukan));
        Guard.Against.Null(kelasRujukan, nameof(kelasRujukan));
        Guard.Against.Null(caraMasukDk, nameof(caraMasukDk));

        RujukanId = rujukanId;
        RujukanName = rujukanName;
        IsAktif = isAktif;
        Alamat = alamat;
        TipeRujukan = tipeRujukan;
        KelasRujukan = kelasRujukan;
        CaraMasukDk = caraMasukDk;
    }
    
    public string RujukanId { get; init; }
    public string RujukanName { get; init; }
    public bool IsAktif { get; init; }
    public AlamatType Alamat { get; init; }
    public TipeRujukanType TipeRujukan { get; init; }
    public KelasRujukanReff KelasRujukan { get; init; }
    public CaraMasukDkType CaraMasukDk { get; init; }
    
    public static IRujukanKey Key(string id) => Default with { RujukanId = id };
    public static RujukanType Default => new RujukanType("-", "-", true, AlamatType.Default, 
        TipeRujukanType.Default, KelasRujukanType.Default.ToReff(), CaraMasukDkType.Default);
}

public interface IRujukanKey
{
    string RujukanId {get;}
}