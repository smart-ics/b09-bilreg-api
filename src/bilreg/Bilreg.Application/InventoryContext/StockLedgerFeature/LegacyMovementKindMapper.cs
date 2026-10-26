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
            ["PK_V"] = MovementKindEnum.InternalConsumptionVoid,
        };

    private static readonly Dictionary<MovementKindEnum, string> ReverseMap =
        new()
        {
            [MovementKindEnum.GoodsReceipt] = "DO",
            [MovementKindEnum.TransferOut] = "MT_OUT",
            [MovementKindEnum.TransferIn] = "MT_IN",
            [MovementKindEnum.SaleIssueDb] = "DB",
            [MovementKindEnum.SaleIssueDu] = "DU",
            [MovementKindEnum.SaleIssueDt] = "DT",
            [MovementKindEnum.InternalConsumption] = "PK",
            [MovementKindEnum.SalesReturnRj] = "RJ",
            [MovementKindEnum.SalesReturnRu] = "RU",
            [MovementKindEnum.SalesReturnRt] = "RT",
            [MovementKindEnum.SaleVoidDb] = "DB_V",
            [MovementKindEnum.SaleVoidDu] = "DU_V",
            [MovementKindEnum.SaleVoidDt] = "DT_V",
            [MovementKindEnum.DispenseIssue] = "DI",
            [MovementKindEnum.InternalConsumptionVoid] = "PK_V",
        };

    public static bool TryMap(string? legacyKind, out MovementKindEnum kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(legacyKind))
            return false;

        return Map.TryGetValue(legacyKind.Trim(), out kind);
    }

    public static bool TryMap(MovementKindEnum kind, out string legacyKind)
    {
        var found = ReverseMap.TryGetValue(kind, out var mapped);
        legacyKind = mapped ?? string.Empty;
        return found;
    }

    public static bool TryMapReverse(MovementKindEnum kind, out string legacyKind)
    {
        return TryMap(kind, out legacyKind);
    }

    public static string ToLegacyString(MovementKindEnum kind)
    {
        if (ReverseMap.TryGetValue(kind, out var legacyKind))
            return legacyKind;
        throw new ArgumentOutOfRangeException(nameof(kind), kind, $"No legacy string mapping for {kind}.");
    }
}
