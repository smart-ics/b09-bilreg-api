using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanFeature;
public record JenisTarifType : IJenisTarifKey
{
    #region CREATION
    public JenisTarifType(string jenisTarifId, string jenisTarifName, int noUrut)
    {
        JenisTarifId = jenisTarifId;
        JenisTarifName = jenisTarifName;
        NoUrut = noUrut;
    }
    public static JenisTarifType Create(string jenisTarifId, string jenisTarifName, int noUrut)
    {
        Guard.Against.NullOrWhiteSpace(jenisTarifId);
        Guard.Against.NullOrWhiteSpace(jenisTarifName);
        Guard.Against.Negative(noUrut);
        return new JenisTarifType(jenisTarifId, jenisTarifName, noUrut);
    }
    public static JenisTarifType Default => new("-", "-", 0);
    public static IJenisTarifKey Key(string id) => Default with { JenisTarifId = id };
    #endregion
    
    #region PROPERTIES
    public string JenisTarifId { get; init; }
    public string JenisTarifName { get; init; }
    public int NoUrut { get; init; }
    #endregion
}

public interface IJenisTarifKey
{
    string JenisTarifId {get;}
}