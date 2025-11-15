using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanFeature;

public record TipeRekType : ITipeRekKey
{
    #region CREATION
    public TipeRekType(string tipeRekId, string tipeRekName)
    {
        TipeRekId = tipeRekId;
        TipeRekName = tipeRekName;
    }
    
    public static TipeRekType Create(string tipeRekId, string tipeRekName)
    {
        Guard.Against.NullOrWhiteSpace(tipeRekId, nameof(tipeRekId));
        Guard.Against.NullOrWhiteSpace(tipeRekName, nameof(tipeRekName));
        return new TipeRekType(tipeRekId, tipeRekName);
    }
    
    public static TipeRekType Default => new("-", "-");
    public static ITipeRekKey Key(string id) => Default with { TipeRekId = id };
    #endregion
    
    #region PROPERTIES
    public string TipeRekId { get; init; }
    public string TipeRekName { get; init; }
    #endregion
}

public interface ITipeRekKey
{
    string TipeRekId { get; }
}