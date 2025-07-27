using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.JaminanSub;

public record JaminanType : IJaminanKey
{
    public JaminanType(string jaminanId, string jaminanName,
        bool isAKtif, AlamatType alamat, CaraBayarDkType caraBayarDk, 
        GroupJaminanReff grupJaminan)
    {
        Guard.Against.NullOrWhiteSpace(jaminanId, nameof(jaminanId));
        Guard.Against.NullOrWhiteSpace(jaminanName, nameof(jaminanName));
        Guard.Against.Null(alamat, nameof(alamat));
        Guard.Against.Null(caraBayarDk, nameof(caraBayarDk));
        Guard.Against.Null(grupJaminan, nameof(grupJaminan));

        JaminanId = jaminanId;
        JaminanName = jaminanName;
        IsAktif = isAKtif;
        Alamat = alamat;
        CaraBayarDk = caraBayarDk;
        GrupJaminan = grupJaminan;
    }
    
    public string JaminanId { get; init; }
    public string JaminanName { get; init; }
    public bool IsAktif { get; init; }
    public AlamatType Alamat { get; init; }
    public CaraBayarDkType CaraBayarDk { get; init; }
    public GroupJaminanReff GrupJaminan { get; init; }

    public JaminanReff ToReff() => new(JaminanId, JaminanName);
    
    public static JaminanType Default => new("-", "-", true, 
        AlamatType.Default, CaraBayarDkType.Default, 
        GroupJaminanType.Default.ToReff());
    public static IJaminanKey Key(string id) => Default with { JaminanId = id };
}

public interface IJaminanKey
{
    string JaminanId {get;}
}

public record JaminanReff(string JaminanId, string JaminanName);