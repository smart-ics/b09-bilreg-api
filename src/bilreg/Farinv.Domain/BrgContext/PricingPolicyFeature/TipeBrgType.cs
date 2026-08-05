using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.PricingPolicyFeature;

public record TipeBrgType : ITipeBrgKey
{
    #region CREATION
    public TipeBrgType(
        string tipeBrgId,
        string tipeBrgName,
        bool isActive,
        decimal biayaPerBarang,
        decimal biayaPerRacik,
        decimal profit,
        decimal tax,
        decimal diskon)
    {
        TipeBrgId = tipeBrgId;
        TipeBrgName = tipeBrgName;
        IsActive = isActive;
        BiayaPerBarang = biayaPerBarang;
        BiayaPerRacik = biayaPerRacik;
        Profit = profit;
        Tax = tax;
        Diskon = diskon;
    }

    public static TipeBrgType Create(
        string tipeBrgId,
        string tipeBrgName,
        bool isActive,
        decimal biayaPerBarang,
        decimal biayaPerRacik,
        decimal profit,
        decimal tax,
        decimal diskon)
    {
        Guard.Against.NullOrWhiteSpace(tipeBrgId, nameof(tipeBrgId));
        Guard.Against.NullOrWhiteSpace(tipeBrgName, nameof(tipeBrgName));
        return new TipeBrgType(
            tipeBrgId,
            tipeBrgName,
            isActive,
            biayaPerBarang,
            biayaPerRacik,
            profit,
            tax,
            diskon);
    }

    public static TipeBrgType Default => new(
        tipeBrgId: "-",
        tipeBrgName: "-",
        isActive: false,
        biayaPerBarang: 0,
        biayaPerRacik: 0,
        profit: 0,
        tax: 0,
        diskon: 0);

    public static ITipeBrgKey Key(string id) => Default with { TipeBrgId = id };
    #endregion

    #region PROPERTIES
    public string TipeBrgId { get; init; }
    public string TipeBrgName { get; init; }
    public bool IsActive { get; init; }
    public decimal BiayaPerBarang { get; init; }
    public decimal BiayaPerRacik { get; init; }
    public decimal Profit { get; init; }
    public decimal Tax { get; init; }
    public decimal Diskon { get; init; }
    #endregion

    #region BEHAVIOUR
    public TipeBrgReff ToReff() => new(TipeBrgId, TipeBrgName);
    #endregion
}

public interface ITipeBrgKey
{
    string TipeBrgId { get; }
}

public record TipeBrgReff(string TipeBrgId, string TipeBrgName);
