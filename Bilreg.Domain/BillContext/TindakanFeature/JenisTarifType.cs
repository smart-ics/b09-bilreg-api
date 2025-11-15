using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanFeature;

public record JenisTarifType : IJenisTarifKey
{
    #region CREATION
    public JenisTarifType(string jenisTarifId, string jenisTarifName)
    {
        JenisTarifId = jenisTarifId;
        JenisTarifName = jenisTarifName;
    }
    public static JenisTarifType Create(string jenisTarifId, string jenisTarifName)
    {
        Guard.Against.NullOrWhiteSpace(jenisTarifId, nameof(jenisTarifId));
        Guard.Against.NullOrWhiteSpace(jenisTarifName, nameof(jenisTarifName));
        return new JenisTarifType(jenisTarifId, jenisTarifName);
    }
    public static JenisTarifType Default => new("-", "-");
    public static IJenisTarifKey Key(string id) => Default with { JenisTarifId = id };
    #endregion
    
    #region PROPERTIES
    public string JenisTarifId { get; init; }
    public string JenisTarifName { get; init; }
    #endregion
}

public interface IJenisTarifKey
{
    string JenisTarifId {get;}
}