using Ardalis.GuardClauses;

namespace Bilreg.Domain.PaymentContext.RekapCetakFeature;

public record RekapCetakType : IRekapCetakKey
{
    public RekapCetakType(string rekapCetakId, string rekapCetakName,
        int noUrut, bool isGrupBaru, int level,
        GroupRekapCetakType groupRekapCetak, RekapCetakDkType rekapCetakDk)
    {
        Guard.Against.NullOrWhiteSpace(rekapCetakId, nameof(rekapCetakId));
        Guard.Against.NullOrWhiteSpace(rekapCetakName, nameof(rekapCetakName));

        RekapCetakId = rekapCetakId;
        RekapCetakName = rekapCetakName;
        NoUrut = noUrut;
        IsGrupBaru = isGrupBaru;
        Level = level;
        GroupRekapCetak = groupRekapCetak;
        RekapCetakDk = rekapCetakDk;
    }
    
    public string RekapCetakId { get; init; }
    public string RekapCetakName { get; init; }
    public int NoUrut { get; init; }
    public bool IsGrupBaru { get; init; }
    public int Level { get; init; }
    public GroupRekapCetakType GroupRekapCetak { get; init; }
    public RekapCetakDkType RekapCetakDk { get; init; }
    
    public RekapCetakReff ToReff() => new(RekapCetakId, RekapCetakName);
    
    public static RekapCetakType Default => new("-", "-", 0, false, 0, 
        GroupRekapCetakType.Default, RekapCetakDkType.Default);
    public static IRekapCetakKey Key(string id) => Default with { RekapCetakId = id };
}

public interface IRekapCetakKey
{
    string RekapCetakId {get;}
}

public record RekapCetakReff(string RekapCetakId, string RekapCetakName);