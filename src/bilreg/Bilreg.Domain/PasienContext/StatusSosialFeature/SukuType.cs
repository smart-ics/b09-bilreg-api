using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.StatusSosialFeature;

public record SukuType : ISukuKey
{
    public SukuType(string sukuId, string sukuName)
    {
        Guard.Against.Null(sukuId, nameof(sukuId));
        Guard.Against.Null(sukuName, nameof(sukuName));

        SukuId = sukuId;
        SukuName = sukuName;
    }
    
    public string SukuId { get; init; }
    public string SukuName { get; init; }
    
    public static SukuType Default => new("-", "-");
    public static ISukuKey Key(string id) => Default with { SukuId = id };
}

public interface ISukuKey
{
    string SukuId {get;}
}
