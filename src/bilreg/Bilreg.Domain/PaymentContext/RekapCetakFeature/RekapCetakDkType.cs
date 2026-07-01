using Ardalis.GuardClauses;

namespace Bilreg.Domain.PaymentContext.RekapCetakFeature;

public record RekapCetakDkType : IRekapCetakDkKey
{
    public RekapCetakDkType(string rekapCetakDkId, string rekapCetakDkName)
    {
        Guard.Against.NullOrWhiteSpace(rekapCetakDkId, nameof(rekapCetakDkId));
        Guard.Against.NullOrWhiteSpace(rekapCetakDkName, nameof(rekapCetakDkName));

        RekapCetakDkId = rekapCetakDkId;
        RekapCetakDkName = rekapCetakDkName;
    }
    
    public string RekapCetakDkId { get; init; }
    public string RekapCetakDkName { get; init; }
    
    
    public static RekapCetakDkType Default => new("-", "-");
    public static IRekapCetakDkKey Key(string id) => Default with { RekapCetakDkId = id };
}

public interface IRekapCetakDkKey
{
    string RekapCetakDkId {get;}
}