using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

public record TarifType : ITarifKey
{
    public TarifType(string tarifId, string tarifName)
    {
        TarifId = tarifId;
        TarifName = tarifName;
    }
    
    public string TarifId { get; init; }
    public string TarifName { get; init; }
    public GroupTarifType GroupTarif { get; init; }
    public GroupTarifDkType GroupTarifDk { get; init; }
    public JenisTarifType JenisTarif { get; init; }
    
    public TarifReff ToReff() => new(TarifId, TarifName);
    
    public static TarifType Default => new("-", "-");
    public static ITarifKey Key(string id) => Default with { TarifId = id };
}

public interface ITarifKey
{
    string TarifId {get;}
}

public record TarifReff(string TarifId, string TarifName);