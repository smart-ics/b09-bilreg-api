namespace Bilreg.Domain.AccountingContext.UnitFeature;

public record UnitGrupType : IUnitGrupKey
{
    public UnitGrupType(string unitGrupId, string unitGrupName,
        int noUrut, bool isRugiLaba)
    {
        UnitGrupId = unitGrupId;
        UnitGrupName = unitGrupName;
        NoUrut = noUrut;
        IsRugiLaba = isRugiLaba;
    }
    public string UnitGrupId { get; init; }
    public string UnitGrupName { get; init; }
    public int NoUrut { get; init; }
    public bool IsRugiLaba { get; init; }
    public UnitGrupReff ToReff() => new(UnitGrupId, UnitGrupName);
    public static UnitGrupType Default => new("-", "-", 0, false);
}

public interface IUnitGrupKey
{
    string UnitGrupId {get;}
}

public record UnitGrupReff(string UnitGrupId, string UnitGrupName);