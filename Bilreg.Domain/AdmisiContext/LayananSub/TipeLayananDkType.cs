using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.LayananSub;

public record TipeLayananDkType : ITipeLayananDkKey
{
    public TipeLayananDkType(string tipeLayananDkId, string tipeLayananDkName)
    {
        Guard.Against.NullOrWhiteSpace(tipeLayananDkId, nameof(tipeLayananDkId));
        Guard.Against.NullOrWhiteSpace(tipeLayananDkName, nameof(tipeLayananDkName));

        TipeLayananDkId = tipeLayananDkId;
        TipeLayananDkName = tipeLayananDkName;
    }
    
    public string TipeLayananDkId { get; init; }
    public string TipeLayananDkName { get; init; }
    
    public static ITipeLayananDkKey Key(string id) => new TipeLayananDkType(id, "-");
    public static TipeLayananDkType Default => new("-", "-");
}

public interface ITipeLayananDkKey
{
    string TipeLayananDkId {get;}
}