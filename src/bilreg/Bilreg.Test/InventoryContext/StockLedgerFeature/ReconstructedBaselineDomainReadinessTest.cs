using System.Reflection;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S3 — Domain readiness for reconstructed baseline facts (BR-STL-065–069).
/// Validates expression of reconstructed Movement / Layer / Position shapes only;
/// does not calculate, claim, or persist reconstruction.
/// </summary>
public class ReconstructedBaselineDomainReadinessTest
{
    private static readonly BrgReff Item = new("BRG01", "Paracetamol");
    private static readonly ReceiptSourceType ReceiptSource = ReceiptSourceType.Create("DO001");
    private static readonly ILayananKey Gudang = LayananType.Key("GDN01");
    private static readonly ILayananKey Apotek = LayananType.Key("APT01");
    private static readonly UnitValuationType Valuation = UnitValuationType.Create(1250.50m);
    private static readonly DateTime EffectiveAt = new(2026, 8, 1, 8, 0, 0);
    private static readonly DateOnly Exp = new(2026, 12, 31);
    private static readonly SourceTransactionReferenceType ReconstructionSource =
        SourceTransactionReferenceType.Create("RECON-BRG01-DO001-fpv1");

    [Fact]
    public void UT01_CreateLayer_ReconstructedOrigin_GeneratesNewAccountableIdentity()
    {
        var movement = StockMovementModel.Key("MOV-RECON-1");

        var layer = StockLayerModel.Create(
            Item,
            ReceiptSource,
            Gudang,
            movement,
            initialQuantity: 10m,
            Valuation,
            EffectiveAt,
            StockFactOriginEnum.Reconstructed,
            Exp);

        layer.Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        layer.StockLayerId.Should().NotBeNullOrWhiteSpace();
        layer.StockLayerId.Length.Should().Be(26);
        layer.InitialQuantity.Should().Be(10m);
        layer.RemainingQuantity.Should().Be(10m);
        layer.IsDepleted.Should().BeFalse();
        layer.LayerFormingMovementId.Should().Be("MOV-RECON-1");
    }

    [Fact]
    public void UT02_CreateLayer_ReconstructedDepleted_RemainingZeroIsRepresentable()
    {
        var layer = StockLayerModel.Create(
            Item,
            ReceiptSource,
            Gudang,
            StockMovementModel.Key("MOV-RECON-DEP"),
            initialQuantity: 8m,
            Valuation,
            EffectiveAt,
            StockFactOriginEnum.Reconstructed,
            Exp,
            remainingQuantity: 0m);

        layer.Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        layer.InitialQuantity.Should().Be(8m);
        layer.RemainingQuantity.Should().Be(0m);
        layer.IsDepleted.Should().BeTrue();
        layer.HasAvailableQuantity.Should().BeFalse();
    }

    [Fact]
    public void UT03_CreateLayer_RemainingExceedsInitial_IsRejected()
    {
        Action act = () => StockLayerModel.Create(
            Item,
            ReceiptSource,
            Gudang,
            StockMovementModel.Key("MOV-BAD"),
            initialQuantity: 5m,
            Valuation,
            EffectiveAt,
            StockFactOriginEnum.Reconstructed,
            remainingQuantity: 6m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Remaining Quantity must not exceed Initial Quantity*");
    }

    [Fact]
    public void UT04_CreateReceipt_ReconstructedOrigin_IsDistinguishableFromNativeAndSync()
    {
        var line = StockMovementLineType.Create(
            1,
            Item,
            ReceiptSource,
            Gudang,
            StockMovementDirectionEnum.Inbound,
            10m,
            Valuation,
            StockFactOriginEnum.Reconstructed);

        var movement = StockMovementModel.CreateReceipt(
            ReconstructionSource,
            EffectiveAt,
            [line],
            StockFactOriginEnum.Reconstructed,
            "MOV-RECON-RCPT");

        movement.Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        movement.Origin.Should().NotBe(StockFactOriginEnum.Native);
        movement.Origin.Should().NotBe(StockFactOriginEnum.LegacySynchronized);
        movement.SourceTransactionId.Should().Be("RECON-BRG01-DO001-fpv1");
        movement.Lines.Single().Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        movement.MovementKind.Should().Be(StockMovementKindEnum.Receipt);
    }

    [Fact]
    public void UT05_BalancedBaselinePayload_ActiveAndDepletedLayersAcrossLocations()
    {
        var formingMovement = StockMovementModel.CreateReceipt(
            ReconstructionSource,
            EffectiveAt,
            [
                StockMovementLineType.Create(
                    1, Item, ReceiptSource, Gudang,
                    StockMovementDirectionEnum.Inbound, 10m, Valuation,
                    StockFactOriginEnum.Reconstructed),
                StockMovementLineType.Create(
                    2, Item, ReceiptSource, Apotek,
                    StockMovementDirectionEnum.Inbound, 5m, Valuation,
                    StockFactOriginEnum.Reconstructed)
            ],
            StockFactOriginEnum.Reconstructed,
            "MOV-RECON-BASE");

        var activeGudang = StockLayerModel.Create(
            Item, ReceiptSource, Gudang, formingMovement,
            initialQuantity: 10m, Valuation, EffectiveAt,
            StockFactOriginEnum.Reconstructed, Exp,
            stockLayerId: "LYR-RECON-GDN-ACTIVE");

        var depletedApotek = StockLayerModel.Create(
            Item, ReceiptSource, Apotek, formingMovement,
            initialQuantity: 5m, Valuation, EffectiveAt.AddDays(1),
            StockFactOriginEnum.Reconstructed, Exp,
            stockLayerId: "LYR-RECON-APT-DEPLETED",
            remainingQuantity: 0m);

        var positionGudang = StockPositionModel
            .CreateEmpty(Item, ReceiptSource, Gudang)
            .AddLayer(activeGudang);

        var positionApotek = StockPositionModel
            .CreateEmpty(Item, ReceiptSource, Apotek)
            .AddLayer(depletedApotek);

        formingMovement.Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        formingMovement.Lines.Should().HaveCount(2);

        positionGudang.TotalRemainingQuantity.Should().Be(10m);
        positionGudang.Layers.Should().ContainSingle();
        positionGudang.Layers[0].Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        positionGudang.Layers[0].IsDepleted.Should().BeFalse();

        positionApotek.TotalRemainingQuantity.Should().Be(0m);
        positionApotek.Layers.Should().ContainSingle();
        positionApotek.Layers[0].IsDepleted.Should().BeTrue();
        positionApotek.Layers[0].Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        positionApotek.Layers[0].InitialQuantity.Should().Be(5m);

        positionGudang.LedgerScope.Should().Be(positionApotek.LedgerScope);
        positionGudang.WriteScope.Should().NotBe(positionApotek.WriteScope);

        // New accountable identities — not invented historical legacy layer ids (BR-STL-065–067).
        positionGudang.Layers[0].StockLayerId.Should().Be("LYR-RECON-GDN-ACTIVE");
        positionApotek.Layers[0].StockLayerId.Should().Be("LYR-RECON-APT-DEPLETED");
        positionGudang.Layers[0].StockLayerId.Should().NotBe(positionApotek.Layers[0].StockLayerId);
    }

    [Fact]
    public void UT06_FactOrigin_IsOrthogonalToReconstructionStatus()
    {
        var scope = StockLedgerScopeStateModel
            .CreateNotReconstructed(Item, ReceiptSource)
            .RequireReconstruction()
            .BeginReconstruction()
            .CompleteReconstruction(
                SynchronizationPositionType.Create(
                    new byte[] { 1, 2, 3, 4 },
                    "fingerprint-v1"),
                reconstructionBasisVersion: "fingerprint-v1");

        scope.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);

        var reconstructedLayer = StockLayerModel.Create(
            Item, ReceiptSource, Gudang, StockMovementModel.Key("MOV-O1"),
            3m, Valuation, EffectiveAt, StockFactOriginEnum.Reconstructed);

        var nativeLayer = StockLayerModel.Create(
            Item, ReceiptSource, Gudang, StockMovementModel.Key("MOV-O2"),
            3m, Valuation, EffectiveAt, StockFactOriginEnum.Native);

        var syncedLayer = StockLayerModel.Create(
            Item, ReceiptSource, Gudang, StockMovementModel.Key("MOV-O3"),
            3m, Valuation, EffectiveAt, StockFactOriginEnum.LegacySynchronized);

        // Scope Reconstruction Status does not dictate or rewrite fact origin.
        reconstructedLayer.Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        nativeLayer.Origin.Should().Be(StockFactOriginEnum.Native);
        syncedLayer.Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);
        scope.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
    }

    [Fact]
    public void UT07_ReconstructionBaseline_CanLinkSourceTransactionAndIdempotency()
    {
        var movement = StockMovementModel.CreateReceipt(
            ReconstructionSource,
            EffectiveAt,
            [
                StockMovementLineType.Create(
                    1, Item, ReceiptSource, Gudang,
                    StockMovementDirectionEnum.Inbound, 4m, Valuation,
                    StockFactOriginEnum.Reconstructed)
            ],
            StockFactOriginEnum.Reconstructed,
            "MOV-RECON-IDEM");

        var idempotency = StockSourceIdempotencyModel.Create(
            StockSourceIdempotencyKindEnum.ReconstructionBaseline,
            idempotencyKey: "RECON|BRG01|DO001|fingerprint-v1",
            processedAt: EffectiveAt,
            sourceTransactionId: movement.SourceTransactionId,
            stockMovementId: movement.StockMovementId,
            brgId: Item.BrgId,
            receiptSourceId: ReceiptSource.ReceiptSourceId);

        idempotency.IdempotencyKind.Should().Be(StockSourceIdempotencyKindEnum.ReconstructionBaseline);
        idempotency.IdempotencyKind.Should().NotBe(StockSourceIdempotencyKindEnum.SourceConsequence);
        idempotency.IdempotencyKind.Should().NotBe(StockSourceIdempotencyKindEnum.SyncBatch);
        idempotency.SourceTransactionId.Should().Be(movement.SourceTransactionId);
        idempotency.StockMovementId.Should().Be(movement.StockMovementId);
        idempotency.BrgId.Should().Be("BRG01");
        idempotency.ReceiptSourceId.Should().Be("DO001");

        Enum.GetNames<StockSourceIdempotencyKindEnum>().Should().BeEquivalentTo(
            "SourceConsequence", "SyncBatch", "ReconstructionBaseline");
    }

    [Fact]
    public void UT08_ReconstructionDoesNotEncodeAuthorityOrInventNewMovementKinds()
    {
        foreach (var type in new[]
                 {
                     typeof(StockLayerModel),
                     typeof(StockMovementModel),
                     typeof(StockPositionModel),
                     typeof(StockFactOriginEnum),
                     typeof(StockSourceIdempotencyKindEnum)
                 })
        {
            type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => m.Name)
                .Should()
                .NotContain(name => name.Contains("Authoritative", StringComparison.OrdinalIgnoreCase),
                    because: $"{type.Name} must not encode authority");
        }

        // Reconstructed baseline reuses existing Receipt kind — no parallel reconstruction movement kind.
        Enum.GetNames<StockMovementKindEnum>().Should().BeEquivalentTo(
            "Receipt", "Outbound", "Transfer", "Correction", "Reversal");
    }

    [Fact]
    public void UT09_CreateLayer_OmittingRemaining_DefaultsToInitialQuantity()
    {
        var layer = StockLayerModel.Create(
            Item,
            ReceiptSource,
            Gudang,
            StockMovementModel.Key("MOV-DEFAULT-REM"),
            initialQuantity: 7m,
            Valuation,
            EffectiveAt,
            StockFactOriginEnum.Reconstructed);

        layer.RemainingQuantity.Should().Be(7m);
        layer.InitialQuantity.Should().Be(7m);
    }
}
