using Ardalis.GuardClauses;

namespace Bilreg.Domain.BillContext.TindakanFeature;

public record TipeTarifType(
    string TipeTarifId, 
    string TipeTarifName, 
    bool IsAktif, 
    int NoUrut): ITipeTarifKey
{
    #region CREATION

    public static TipeTarifType Create(string tipeTarifId, string tipeTarifName, bool isAktif, int noUrut)
    {
        Guard.Against.NullOrWhiteSpace(tipeTarifId, nameof(tipeTarifId));
        Guard.Against.NullOrWhiteSpace(tipeTarifName, nameof(tipeTarifName));
        Guard.Against.Negative(noUrut, nameof(noUrut));
        return new TipeTarifType(tipeTarifId, tipeTarifName, isAktif, noUrut);
    }
    
    public static TipeTarifType Default => new("-", "-", true, 0);
    public static ITipeTarifKey Key(string id) => Default with { TipeTarifId = id };
    #endregion
    
    public TipeTarifReff ToReff()
        => new TipeTarifReff(TipeTarifId, TipeTarifName);
}

public interface ITipeTarifKey
{
    string TipeTarifId { get; }
}

public record TipeTarifReff(string TipeTarifId, string TipeTarifName);