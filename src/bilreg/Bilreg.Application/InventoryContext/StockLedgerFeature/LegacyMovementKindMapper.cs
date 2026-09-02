using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Anti-corruption map of legacy <c>fs_kd_jenis_mutasi</c> strings to S1 MovementKind INT values
/// (GAP-STL-003 interim). Unknown strings must not invent kinds.
/// </summary>
public static class LegacyMovementKindMapper
{
    private static readonly Dictionary<string, MovementKindEnum> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["DO"] = MovementKindEnum.GoodsReceipt,
            ["MT_OUT"] = MovementKindEnum.TransferOut,
            ["MT_IN"] = MovementKindEnum.TransferIn,
            ["DB"] = MovementKindEnum.SaleIssueDb,
            ["DU"] = MovementKindEnum.SaleIssueDu,
            ["DT"] = MovementKindEnum.SaleIssueDt,
            ["PK"] = MovementKindEnum.InternalConsumption,
            ["RJ"] = MovementKindEnum.SalesReturnRj,
            ["RU"] = MovementKindEnum.SalesReturnRu,
            ["RT"] = MovementKindEnum.SalesReturnRt,
            ["DB_V"] = MovementKindEnum.SaleVoidDb,
            ["DU_V"] = MovementKindEnum.SaleVoidDu,
            ["DT_V"] = MovementKindEnum.SaleVoidDt,
            ["DI"] = MovementKindEnum.DispenseIssue,
        };

    public static bool TryMap(string? legacyKind, out MovementKindEnum kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(legacyKind))
            return false;

        return Map.TryGetValue(legacyKind.Trim(), out kind);
    }
}
