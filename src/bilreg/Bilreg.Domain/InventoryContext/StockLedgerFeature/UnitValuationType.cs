using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Inventory value per unit retained with a stock quantity (BR-STL-019).
/// Not total layer/movement value.
/// </summary>
public record UnitValuationType
{
    #region CREATION
    public UnitValuationType(decimal amountPerUnit)
    {
        Guard.Against.Negative(amountPerUnit, nameof(amountPerUnit));
        AmountPerUnit = amountPerUnit;
    }

    public static UnitValuationType Create(decimal amountPerUnit)
        => new(amountPerUnit);

    public static UnitValuationType Zero => new(0m);
    #endregion

    #region PROPERTIES
    public decimal AmountPerUnit { get; init; }
    #endregion
}
