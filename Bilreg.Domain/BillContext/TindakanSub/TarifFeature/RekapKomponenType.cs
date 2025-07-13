using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

public record RekapKomponenType : IRekapKomponenKey
{
    public RekapKomponenType(string rekapKomponenId, string rekapKomponenName, int noUrut)
    {
        Guard.Against.NullOrWhiteSpace(rekapKomponenId, nameof(rekapKomponenId));
        Guard.Against.NullOrWhiteSpace(rekapKomponenName, nameof(rekapKomponenName));
        Guard.Against.NegativeOrZero(noUrut, nameof(noUrut));

        RekapKomponenId = rekapKomponenId;
        RekapKomponenName = rekapKomponenName;
        NoUrut = noUrut;
    }
    
    public string RekapKomponenId { get; init; }
    public string RekapKomponenName { get; init; }
    public int NoUrut { get; init; }

    public static RekapKomponenType Default => new("-", "-",0);
    public static IRekapKomponenKey Key(string id) => Default with { RekapKomponenId = id };
}

public interface IRekapKomponenKey
{
    string RekapKomponenId {get;}
}