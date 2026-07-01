namespace Bilreg.Domain.AccountingContext.UnitFeature;

public record UnitType : IUnitKey
{
    public UnitType(string unitId, string unitName,
        int noUrut, bool isRugiLaba, UnitGrupReff unitGrup)
    {
        UnitId = unitId;
        UnitName = unitName;
        NoUrut = noUrut;
        IsRugiLaba = isRugiLaba;
        UnitGrup = unitGrup;
    }
    public string UnitId { get; init; }
    public string UnitName { get; init; }
    public int NoUrut { get; init; }
    public bool IsRugiLaba { get; init; }
    public UnitGrupReff UnitGrup { get; init; }

    public static UnitType Default => new("-", "-", 0, false, UnitGrupType.Default.ToReff());
    public UnitReff ToReff() => new(UnitId, UnitName);
}

public interface IUnitKey
{
    string UnitId {get;}
}

public record UnitReff(string UnitId, string UnitName);