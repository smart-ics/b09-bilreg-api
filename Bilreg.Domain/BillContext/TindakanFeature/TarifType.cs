namespace Bilreg.Domain.BillContext.TindakanFeature;

public record TarifType : ITarifKey
{
    #region CREATION
    public TarifType(string tarifId, string tarifName, 
        GroupTarifType groupTarif, GroupTarifDkType groupTarifDk, 
        JenisTarifType jenisTarif)
    {
        TarifId = tarifId;
        TarifName = tarifName;
        GroupTarif = groupTarif;
        GroupTarifDk = groupTarifDk;
        JenisTarif = jenisTarif;
    }
    public static TarifType Default => new TarifType("-", "-", GroupTarifType.Default,
        GroupTarifDkType.Default, JenisTarifType.Default);
    public static ITarifKey Key(string id) => Default with { TarifId = id };
    #endregion


    public string TarifId { get; init; }
    public string TarifName { get; init; }
    public GroupTarifType GroupTarif { get; init; }
    public GroupTarifDkType GroupTarifDk { get; init; }
    public JenisTarifType JenisTarif { get; init; }
    public TarifReff ToReff() => new(TarifId, TarifName);
}

public interface ITarifKey
{
    string TarifId {get;}
}

public record TarifReff(string TarifId, string TarifName);