using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Domain.AdmisiContext.JaminanFeature;

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
        GroupJaminan = grupJaminan;
    }
    
    public string JaminanId { get; init; }
    public string JaminanName { get; init; }
    public bool IsAktif { get; init; }
    public AlamatType Alamat { get; init; }
    public CaraBayarDkType CaraBayarDk { get; init; }
    public GroupJaminanReff GroupJaminan { get; init; }

    public JaminanReff ToReff() => new(JaminanId, JaminanName);
    public CoaType RekPiutangJasa
    
    public static JaminanType Default => new("-", "-", true, 
        AlamatType.Default, CaraBayarDkType.Default, 
        GroupJaminanType.Default.ToReff());
    public static IJaminanKey Key(string id) => Default with { JaminanId = id };
    public static JaminanType Umum => new("000", "Umum", true, 
        AlamatType.Default, CaraBayarDkType.BayarSendiri, 
        GroupJaminanType.Default.ToReff());
}

public interface IJaminanKey
{
    string JaminanId {get;}
}

public record JaminanReff(string JaminanId, string JaminanName);

