using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S4 / R-003 — Pure projection of interpreter intents onto positions.
/// </summary>
public class LegacySyncIntentApplicatorTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Unspecified);
    private static readonly IStockLedgerScopeKey Scope =
        StockLedgerScopeKeyType.Create("BRG-S4-APP", "DO-S4-APP");

    [Fact]
    public void RepresentationalBalanceOmission_IsQuantityNeutral()
    {
        var positions = new Dictionary<string, StockPositionModel>(StringComparer.Ordinal);
        var intent = new LegacySyncIntentType(
            LegacySyncIntentKindEnum.RepresentationalBalanceOmission,
            LegacyJournalId: null,
            LayananId: "LY01",
            LegacyRowId: "STK1",
            TargetMovementId: null,
            TargetLayerId: "LYR1",
            ProposedMovement: null,
            SyncIdempotencyKey: "SYNC|STOK|…|OMISSION",
            Explanation: "omission");

        var result = LegacySyncIntentApplicator.Apply(intent, Scope, positions);

        result.Movement.Should().BeNull();
        result.Positions.Should().BeEmpty();
        positions.Should().BeEmpty();
    }

    [Fact]
    public void AdjustLayerRemainingQuantity_PreservesLayerOrigin()
    {
        var layer = StockLayerModel.Create(
            BrgObatType.Key(Scope.BrgId),
            ReceiptSourceType.Create(Scope.ReceiptSourceId),
            LayananType.Key("LY01"),
            StockMovementModel.Key("MOV-BASE"),
            initialQuantity: 10m,
            UnitValuationType.Create(1000m),
            T1,
            StockFactOriginEnum.Reconstructed,
            stockLayerId: "LYR-RECON");
        var position = StockPositionModel.CreateEmpty(
                BrgObatType.Key(Scope.BrgId),
                ReceiptSourceType.Create(Scope.ReceiptSourceId),
                LayananType.Key("LY01"))
            .AddLayer(layer);
        var positions = new Dictionary<string, StockPositionModel>(StringComparer.Ordinal)
        {
            ["LY01"] = position
        };

        var line = StockMovementLineType.Create(
            1,
            BrgObatType.Key(Scope.BrgId),
            ReceiptSourceType.Create(Scope.ReceiptSourceId),
            LayananType.Key("LY01"),
            StockMovementDirectionEnum.Outbound,
            3m,
            UnitValuationType.Create(1000m),
            StockFactOriginEnum.LegacySynchronized,
            StockLayerModel.Key("LYR-RECON"));
        var movement = StockMovementModel.CreateOutbound(
            SourceTransactionReferenceType.Create("SYNC-BAL|dec"),
            T1,
            [line],
            StockFactOriginEnum.LegacySynchronized,
            "MOV-ADJ");

        var intent = new LegacySyncIntentType(
            LegacySyncIntentKindEnum.AdjustLayerRemainingQuantity,
            null,
            "LY01",
            "STK1",
            null,
            "LYR-RECON",
            movement,
            "SYNC|STOK|key",
            null);

        var result = LegacySyncIntentApplicator.Apply(intent, Scope, positions);

        result.Movement.Should().NotBeNull();
        var updated = positions["LY01"].Layers.Single(l => l.StockLayerId == "LYR-RECON");
        updated.Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        updated.RemainingQuantity.Should().Be(7m);
    }

    [Fact]
    public void ApplyLegacySynchronizedReceipt_CreatesLegacySynchronizedLayer()
    {
        var positions = new Dictionary<string, StockPositionModel>(StringComparer.Ordinal);
        var line = StockMovementLineType.Create(
            1,
            BrgObatType.Key(Scope.BrgId),
            ReceiptSourceType.Create(Scope.ReceiptSourceId),
            LayananType.Key("LY01"),
            StockMovementDirectionEnum.Inbound,
            5m,
            UnitValuationType.Create(1000m),
            StockFactOriginEnum.LegacySynchronized);
        var movement = StockMovementModel.CreateReceipt(
            SourceTransactionReferenceType.Create("SYNC-INS"),
            T1,
            [line],
            StockFactOriginEnum.LegacySynchronized,
            "MOV-INS");

        var intent = new LegacySyncIntentType(
            LegacySyncIntentKindEnum.ApplyLegacySynchronizedReceipt,
            "TRS-NEW",
            "LY01",
            null,
            null,
            null,
            movement,
            "SYNC|BUKU|key",
            null);

        var result = LegacySyncIntentApplicator.Apply(intent, Scope, positions);

        result.Movement!.Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);
        positions["LY01"].Layers.Should().ContainSingle();
        positions["LY01"].Layers[0].Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);
        positions["LY01"].Layers[0].RemainingQuantity.Should().Be(5m);
    }

    [Fact]
    public void HasCompleteCoverage_RequiresAllMaterialKeys()
    {
        var journals = new[]
        {
            new LegacyStockJournalEntryType(
                "J1", Scope.BrgId, Scope.ReceiptSourceId, "LY01",
                10m, 0m, 1000m, null, null, "DO", "J1", T1, null)
        };
        var balances = new[]
        {
            new LegacyStockBalanceType(
                Scope.BrgId, Scope.ReceiptSourceId, "LY01",
                10m, 1000m, null, null, null, "ST1", T1, T1)
        };

        var journalKey = LegacyChangeDiscoveryIdentityKeys.BuildJournalKey(Scope, journals[0]);
        var balanceKey = LegacyChangeDiscoveryIdentityKeys.BuildBalanceKey(Scope, balances[0]);

        LegacySyncIdentityBootstrapper.HasCompleteCoverage(
            Scope, journals, balances, [journalKey]).Should().BeFalse();

        LegacySyncIdentityBootstrapper.HasCompleteCoverage(
            Scope, journals, balances, [journalKey, balanceKey]).Should().BeTrue();
    }
}
