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

            // Legacy VB6 clbGenStokX1.cls mappings
            ["AJ_PLUS"] = MovementKindEnum.AdjustmentPlus,
            ["AJ_PLUS_V"] = MovementKindEnum.AdjustmentPlusVoid,
            ["AJ_MIN"] = MovementKindEnum.AdjustmentMinus,
            ["AJ_MIN_V"] = MovementKindEnum.AdjustmentMinusVoid,
            ["DO_V"] = MovementKindEnum.GoodsReceiptVoid,
            ["MT_OUT_V"] = MovementKindEnum.TransferOutVoid,
            ["MT_IN_V"] = MovementKindEnum.TransferInVoid,
            ["RJ_V"] = MovementKindEnum.SalesReturnRjVoid,
            ["RU_V"] = MovementKindEnum.SalesReturnRuVoid,
            ["RT_V"] = MovementKindEnum.SalesReturnRtVoid,
            ["RP_ASAL"] = MovementKindEnum.RepackSource,
            ["RP_ASAL_V"] = MovementKindEnum.RepackSourceVoid,
            ["RP_HASIL"] = MovementKindEnum.RepackOutput,
            ["RP_HASIL_V"] = MovementKindEnum.RepackOutputVoid,
            ["MN"] = MovementKindEnum.DisposalIssue,
            ["MN_V"] = MovementKindEnum.DisposalIssueVoid,
            ["RB"] = MovementKindEnum.PurchaseReturn,
            ["RB_V"] = MovementKindEnum.PurchaseReturnVoid,
            ["DU-S"] = MovementKindEnum.SatelliteIssue,
            ["DU-S_V"] = MovementKindEnum.SatelliteIssueVoid,
            ["DU-R_IN"] = MovementKindEnum.SatelliteReserveIn,
            ["DU-R_IN_V"] = MovementKindEnum.SatelliteReserveInVoid,
            ["DU-R_OUT"] = MovementKindEnum.SatelliteReserveOut,
            ["DU-R_OUT_V"] = MovementKindEnum.SatelliteReserveOutVoid,
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

            // Legacy VB6 clbGenStokX1.cls reverse mappings
            [MovementKindEnum.AdjustmentPlus] = "AJ_PLUS",
            [MovementKindEnum.AdjustmentPlusVoid] = "AJ_PLUS_V",
            [MovementKindEnum.AdjustmentMinus] = "AJ_MIN",
            [MovementKindEnum.AdjustmentMinusVoid] = "AJ_MIN_V",
            [MovementKindEnum.GoodsReceiptVoid] = "DO_V",
            [MovementKindEnum.TransferOutVoid] = "MT_OUT_V",
            [MovementKindEnum.TransferInVoid] = "MT_IN_V",
            [MovementKindEnum.SalesReturnRjVoid] = "RJ_V",
            [MovementKindEnum.SalesReturnRuVoid] = "RU_V",
            [MovementKindEnum.SalesReturnRtVoid] = "RT_V",
            [MovementKindEnum.RepackSource] = "RP_ASAL",
            [MovementKindEnum.RepackSourceVoid] = "RP_ASAL_V",
            [MovementKindEnum.RepackOutput] = "RP_HASIL",
            [MovementKindEnum.RepackOutputVoid] = "RP_HASIL_V",
            [MovementKindEnum.DisposalIssue] = "MN",
            [MovementKindEnum.DisposalIssueVoid] = "MN_V",
            [MovementKindEnum.PurchaseReturn] = "RB",
            [MovementKindEnum.PurchaseReturnVoid] = "RB_V",
            [MovementKindEnum.SatelliteIssue] = "DU-S",
            [MovementKindEnum.SatelliteIssueVoid] = "DU-S_V",
            [MovementKindEnum.SatelliteReserveIn] = "DU-R_IN",
            [MovementKindEnum.SatelliteReserveInVoid] = "DU-R_IN_V",
            [MovementKindEnum.SatelliteReserveOut] = "DU-R_OUT",
            [MovementKindEnum.SatelliteReserveOutVoid] = "DU-R_OUT_V",
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
