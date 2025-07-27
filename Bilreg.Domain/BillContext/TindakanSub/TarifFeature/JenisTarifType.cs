using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

public record JenisTarifType : IJenisTarifKey
{
    public JenisTarifType(string jenisTarifId, string jenisTarifName)
    {
        Guard.Against.NullOrWhiteSpace(jenisTarifId, nameof(jenisTarifId));
        Guard.Against.NullOrWhiteSpace(jenisTarifName, nameof(jenisTarifName));

        JenisTarifId = jenisTarifId;
        JenisTarifName = jenisTarifName;
    }
    
    public string JenisTarifId { get; init; }
    public string JenisTarifName { get; init; }
    
    public static JenisTarifType Default => new("-", "-");
    public static IJenisTarifKey Key(string id) => Default with { JenisTarifId = id };
}

public interface IJenisTarifKey
{
    string JenisTarifId {get;}
}