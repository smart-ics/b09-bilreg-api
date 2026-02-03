using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.RujukanFeature;

public record RujukanType : IRujukanKey, IPpkKey
{
    #region CREATION
    public RujukanType(string rujukanId, string rujukanName, bool isAktif, string ppkId,
        AlamatType alamat, ContactType noTelp, TipeRujukanType tipeRujukan,
        KelasRujukanReff kelasRujukan, CaraMasukDkType caraMasukDk)
    {
        RujukanId = rujukanId;
        RujukanName = rujukanName;
        IsAktif = isAktif;
        PpkId = ppkId;
        Alamat = alamat;
        NoTelp = noTelp;
        TipeRujukan = tipeRujukan;
        KelasRujukan = kelasRujukan;
        CaraMasukDk = caraMasukDk;
    }
    public static IRujukanKey Key(string id) => Default with { RujukanId = id };
    public static RujukanType Default => new RujukanType("-", "-", true, "-", AlamatType.Default, 
        ContactType.Default,  TipeRujukanType.Default, KelasRujukanType.Default.ToReff(), 
        CaraMasukDkType.Default);

    public static RujukanType Create(string rujukanId, string rujukanName, bool isAktif, string ppkId,
        AlamatType alamat, ContactType noTelp, TipeRujukanType tipeRujukan,
        KelasRujukanReff kelasRujukan, CaraMasukDkType caraMasukDk)
    {
        Guard.Against.NullOrWhiteSpace(rujukanId, nameof(rujukanId));
        Guard.Against.NullOrWhiteSpace(rujukanName, nameof(rujukanName));
        Guard.Against.Null(alamat, nameof(alamat));
        Guard.Against.Null(noTelp, nameof(noTelp));
        Guard.Against.Null(tipeRujukan, nameof(tipeRujukan));
        Guard.Against.Null(kelasRujukan, nameof(kelasRujukan));
        Guard.Against.Null(caraMasukDk, nameof(caraMasukDk));

        return new RujukanType(rujukanId, rujukanName, isAktif, ppkId, alamat, 
            noTelp, tipeRujukan, kelasRujukan, caraMasukDk);
    }
    #endregion
    
    public string RujukanId { get; init; }
    public string RujukanName { get; init; }
    public bool IsAktif { get; init; }
    public string PpkId { get; init; }
    public AlamatType Alamat { get; init; }
    public ContactType NoTelp { get; init; }
    public TipeRujukanType TipeRujukan { get; init; }
    public KelasRujukanReff KelasRujukan { get; init; }
    public CaraMasukDkType CaraMasukDk { get; init; }

    public RujukanReff ToReff() => new RujukanReff(RujukanId, RujukanName);
}

public interface IRujukanKey
{
    string RujukanId {get;}
}

public interface IPpkKey
{
    string PpkId {get;}
}

public record RujukanReff(string RujukanId, string RujukanName);