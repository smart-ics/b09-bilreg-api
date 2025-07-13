using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext;

public record SukuType : ISukuKey
{
    public SukuType(string sukuId, string sukuName)
    {
        Guard.Against.NullOrWhiteSpace(sukuId, nameof(sukuId));
        Guard.Against.NullOrWhiteSpace(sukuName, nameof(sukuName));

        SukuId = sukuId;
        SukuName = sukuName;
    }
    
    public string SukuId { get; init; }
    public string SukuName { get; init; }
    
    public static ISukuKey Key(string id) => new SukuType(id, "-");
    public static SukuType Default => new("-", "-");
}

public interface ISukuKey
{
    string SukuId {get;}
}