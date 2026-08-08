using System.Reflection;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockLayerPositionFifoTest
{
    private static readonly BrgReff Item = new("BRG01", "Paracetamol");
    private static readonly BrgReff OtherItem = new("BRG99", "Other");
    private static readonly ReceiptSourceType ReceiptSource = ReceiptSourceType.Create("DO001");
    private static readonly ReceiptSourceType OtherReceiptSource = ReceiptSourceType.Create("DO002");
    private static readonly ILayananKey Gudang = LayananType.Key("GDN01");
    private static readonly ILayananKey Apotek = LayananType.Key("APT01");
    private static readonly UnitValuationType ValuationA = UnitValuationType.Create(1000m);
    private static readonly UnitValuationType ValuationB = UnitValuationType.Create(1500m);
    private static readonly DateTime ReceiptT1 = new(2026, 8, 1, 8, 0, 0);
    private static readonly DateTime ReceiptT2 = new(2026, 8, 2, 8, 0, 0);
    private static readonly DateTime ReceiptT3 = new(2026, 8, 3, 8, 0, 0);
    private static readonly DateOnly ExpAug = new(2026, 12, 31);
    private static readonly DateOnly ExpSep = new(2027, 1, 31);

    [Fact]
    public void UT01_CreateLayer_ValidQuantity_Succeeds()
    {
        var layer = Layer("LYR-1", 10m, ReceiptT1, ExpAug, ValuationA);

        layer.StockLayerId.Should().Be("LYR-1");
        layer.InitialQuantity.Should().Be(10m);
        layer.RemainingQuantity.Should().Be(10m);
        layer.IsDepleted.Should().BeFalse();
        layer.UnitValuation.AmountPerUnit.Should().Be(1000m);
        layer.ExpirationDate.Should().Be(ExpAug);
        layer.EffectiveReceiptTime.Should().Be(ReceiptT1);
        layer.ReceiptSourceId.Should().Be("DO001");
        layer.Origin.Should().Be(StockFactOriginEnum.Native);
    }

    [Fact]
    public void UT02_CreateLayer_NonPositiveInitialQuantity_IsRejected()
    {
        Action zero = () => Layer("LYR-Z", 0m, ReceiptT1, ExpAug, ValuationA);
        Action negative = () => Layer("LYR-N", -1m, ReceiptT1, ExpAug, ValuationA);

        zero.Should().Throw<ArgumentException>();
        negative.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UT03_Allocate_NoExpirationDate_UsesFifoByEffectiveReceiptTime()
    {
        var layers = new[]
        {
            Layer("LYR-LATE", 5m, ReceiptT3, ExpSep, ValuationB),
            Layer("LYR-EARLY", 5m, ReceiptT1, ExpAug, ValuationA),
            Layer("LYR-MID", 5m, ReceiptT2, ExpSep, ValuationA)
        };

        var result = StockFifoAllocator.Allocate(layers, Item, Gudang, 7m);

        result.IsFulfilled.Should().BeTrue();
        result.Allocations.Should().HaveCount(2);
        result.Allocations[0].StockLayerId.Should().Be("LYR-EARLY");
        result.Allocations[0].Quantity.Should().Be(5m);
        result.Allocations[1].StockLayerId.Should().Be("LYR-MID");
        result.Allocations[1].Quantity.Should().Be(2m);
    }

    [Fact]
    public void UT04_Allocate_ExplicitExpirationDate_OnlyMatchingLayersEligible()
    {
        var layers = new[]
        {
            Layer("LYR-AUG", 10m, ReceiptT1, ExpAug, ValuationA),
            Layer("LYR-SEP", 10m, ReceiptT2, ExpSep, ValuationB)
        };

        var result = StockFifoAllocator.Allocate(layers, Item, Gudang, 5m, ExpSep);

        result.IsFulfilled.Should().BeTrue();
        result.Allocations.Should().ContainSingle();
        result.Allocations[0].StockLayerId.Should().Be("LYR-SEP");
        result.Allocations[0].ExpirationDate.Should().Be(ExpSep);
        result.UpdatedLayers.Single(x => x.StockLayerId == "LYR-AUG").RemainingQuantity.Should().Be(10m);
        result.UpdatedLayers.Single(x => x.StockLayerId == "LYR-SEP").RemainingQuantity.Should().Be(5m);
    }

    [Fact]
    public void UT05_Allocate_EqualEffectiveReceiptTime_UsesLayerIdTieBreak()
    {
        var layers = new[]
        {
            Layer("LYR-B", 4m, ReceiptT1, ExpAug, ValuationA),
            Layer("LYR-A", 4m, ReceiptT1, ExpAug, ValuationA),
            Layer("LYR-C", 4m, ReceiptT1, ExpAug, ValuationA)
        };

        var result = StockFifoAllocator.Allocate(layers, Item, Gudang, 5m);

        result.IsFulfilled.Should().BeTrue();
        result.Allocations[0].StockLayerId.Should().Be("LYR-A");
        result.Allocations[0].Quantity.Should().Be(4m);
        result.Allocations[1].StockLayerId.Should().Be("LYR-B");
        result.Allocations[1].Quantity.Should().Be(1m);
    }

    [Fact]
    public void UT06_Allocate_MultiLayer_ConsumesAcrossLayers()
    {
        var layers = new[]
        {
            Layer("LYR-1", 3m, ReceiptT1, ExpAug, ValuationA),
            Layer("LYR-2", 3m, ReceiptT2, ExpAug, ValuationB),
            Layer("LYR-3", 3m, ReceiptT3, ExpAug, ValuationA)
        };

        var result = StockFifoAllocator.Allocate(layers, Item, Gudang, 8m);

        result.IsFulfilled.Should().BeTrue();
        result.Allocations.Should().HaveCount(3);
        result.Allocations.Select(x => x.StockLayerId).Should().Equal("LYR-1", "LYR-2", "LYR-3");
        result.Allocations.Select(x => x.Quantity).Should().Equal(3m, 3m, 2m);
        result.AllocatedQuantity.Should().Be(8m);
    }

    [Fact]
    public void UT07_Allocate_ExactQuantity_FullyConsumesLayer()
    {
        var layers = new[] { Layer("LYR-EXACT", 6m, ReceiptT1, ExpAug, ValuationA) };

        var result = StockFifoAllocator.Allocate(layers, Item, Gudang, 6m);

        result.IsFulfilled.Should().BeTrue();
        result.Allocations.Should().ContainSingle();
        result.Allocations[0].Quantity.Should().Be(6m);
        var updated = result.UpdatedLayers.Single();
        updated.RemainingQuantity.Should().Be(0m);
        updated.IsDepleted.Should().BeTrue();
        updated.InitialQuantity.Should().Be(6m);
    }

    [Fact]
    public void UT08_Allocate_InsufficientStock_ReturnsUnfulfilledWithoutMutation()
    {
        var layers = new[]
        {
            Layer("LYR-1", 2m, ReceiptT1, ExpAug, ValuationA),
            Layer("LYR-2", 2m, ReceiptT2, ExpAug, ValuationA)
        };

        var result = StockFifoAllocator.Allocate(layers, Item, Gudang, 10m);

        result.IsFulfilled.Should().BeFalse();
        result.AllocatedQuantity.Should().Be(0m);
        result.UnfulfilledQuantity.Should().Be(10m);
        result.Allocations.Should().BeEmpty();
        result.UpdatedLayers.Should().BeEmpty();
        layers[0].RemainingQuantity.Should().Be(2m);
        layers[1].RemainingQuantity.Should().Be(2m);
    }

    [Fact]
    public void UT09_Consume_CannotProduceNegativeRemainingQuantity()
    {
        var layer = Layer("LYR-1", 3m, ReceiptT1, ExpAug, ValuationA);

        Action act = () => layer.Consume(4m);

        act.Should().Throw<ArgumentException>();
        layer.RemainingQuantity.Should().Be(3m);
    }

    [Fact]
    public void UT10_DepletedLayer_IsRetainedAtZero()
    {
        var position = StockPositionModel.CreateEmpty(Item, ReceiptSource, Gudang)
            .AddLayer(Layer("LYR-1", 5m, ReceiptT1, ExpAug, ValuationA));

        var (result, next) = position.Allocate(5m);

        result.IsFulfilled.Should().BeTrue();
        next.Layers.Should().ContainSingle();
        next.Layers[0].StockLayerId.Should().Be("LYR-1");
        next.Layers[0].RemainingQuantity.Should().Be(0m);
        next.Layers[0].IsDepleted.Should().BeTrue();
        next.TotalRemainingQuantity.Should().Be(0m);
        next.Version.Should().Be(position.Version + 1);
    }

    [Fact]
    public void UT11_DepletedLayer_ExcludedFromLaterAllocation()
    {
        var position = StockPositionModel.CreateEmpty(Item, ReceiptSource, Gudang)
            .AddLayer(Layer("LYR-OLD", 4m, ReceiptT1, ExpAug, ValuationA))
            .AddLayer(Layer("LYR-NEW", 4m, ReceiptT2, ExpAug, ValuationB));

        var (first, afterFirst) = position.Allocate(4m);
        first.IsFulfilled.Should().BeTrue();
        first.Allocations.Single().StockLayerId.Should().Be("LYR-OLD");

        var (second, afterSecond) = afterFirst.Allocate(3m);
        second.IsFulfilled.Should().BeTrue();
        second.Allocations.Should().ContainSingle();
        second.Allocations[0].StockLayerId.Should().Be("LYR-NEW");
        second.Allocations[0].Quantity.Should().Be(3m);

        afterSecond.Layers.Single(x => x.StockLayerId == "LYR-OLD").RemainingQuantity.Should().Be(0m);
        afterSecond.Layers.Single(x => x.StockLayerId == "LYR-NEW").RemainingQuantity.Should().Be(1m);
    }

    [Fact]
    public void UT12_Allocation_PreservesUnitValuationAndReceiptSource()
    {
        var layers = new[]
        {
            Layer("LYR-A", 2m, ReceiptT1, ExpAug, ValuationA, receiptSource: ReceiptSource),
            Layer("LYR-B", 2m, ReceiptT2, ExpAug, ValuationB, receiptSource: OtherReceiptSource)
        };

        var result = StockFifoAllocator.Allocate(layers, Item, Gudang, 3m);

        result.Allocations[0].ReceiptSourceId.Should().Be("DO001");
        result.Allocations[0].UnitValuation.AmountPerUnit.Should().Be(1000m);
        result.Allocations[1].ReceiptSourceId.Should().Be("DO002");
        result.Allocations[1].UnitValuation.AmountPerUnit.Should().Be(1500m);
    }

    [Fact]
    public void UT13_Allocate_BatchIsNotUsedForSelectionOrOrder()
    {
        var layers = new[]
        {
            Layer("LYR-2", 5m, ReceiptT2, ExpAug, ValuationA, batch: "BATCH-AAA"),
            Layer("LYR-1", 5m, ReceiptT1, ExpAug, ValuationA, batch: "BATCH-ZZZ")
        };

        var result = StockFifoAllocator.Allocate(layers, Item, Gudang, 6m);

        // Earlier Effective Receipt Time wins despite lexicographically later Batch.
        result.Allocations[0].StockLayerId.Should().Be("LYR-1");
        result.Allocations[0].Quantity.Should().Be(5m);
        result.Allocations[1].StockLayerId.Should().Be("LYR-2");
        result.Allocations[1].Quantity.Should().Be(1m);

        typeof(StockFifoAllocator)
            .GetMethod(nameof(StockFifoAllocator.Allocate))!
            .GetParameters()
            .Select(p => p.Name)
            .Should()
            .NotContain(name => name!.Contains("Batch", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UT14_Allocation_IdentifiesStockLayersOnOutboundMovementLines()
    {
        var layers = new[]
        {
            Layer("LYR-1", 2m, ReceiptT1, ExpAug, ValuationA),
            Layer("LYR-2", 3m, ReceiptT2, ExpSep, ValuationB)
        };

        var result = StockFifoAllocator.Allocate(layers, Item, Gudang, 4m);
        var lines = result.ToOutboundMovementLines();

        lines.Should().HaveCount(2);
        lines[0].StockLayerId.Should().Be("LYR-1");
        lines[0].Quantity.Should().Be(2m);
        lines[0].Direction.Should().Be(StockMovementDirectionEnum.Outbound);
        lines[1].StockLayerId.Should().Be("LYR-2");
        lines[1].Quantity.Should().Be(2m);

        var movement = StockMovementModel.CreateOutbound(
            SourceTransactionReferenceType.Create("OUT-001"),
            ReceiptT3,
            lines,
            StockFactOriginEnum.Native,
            "MOV-OUT-ALLOC");

        movement.Lines.Select(x => x.StockLayerId).Should().Equal("LYR-1", "LYR-2");
    }

    [Fact]
    public void UT15_Allocate_IgnoresOtherItemAndLocation()
    {
        var layers = new[]
        {
            Layer("LYR-OK", 5m, ReceiptT1, ExpAug, ValuationA),
            Layer("LYR-OTHER-ITEM", 50m, ReceiptT1, ExpAug, ValuationA, item: OtherItem),
            Layer("LYR-OTHER-LOC", 50m, ReceiptT1, ExpAug, ValuationA, location: Apotek)
        };

        var result = StockFifoAllocator.Allocate(layers, Item, Gudang, 5m);

        result.IsFulfilled.Should().BeTrue();
        result.Allocations.Should().ContainSingle();
        result.Allocations[0].StockLayerId.Should().Be("LYR-OK");
    }

    [Fact]
    public void UT16_Position_WriteScopeIsItemReceiptSourceAndLocation()
    {
        var position = StockPositionModel.CreateEmpty(Item, ReceiptSource, Gudang);

        position.WriteScope.Should().Be(StockWriteScopeKeyType.Create("BRG01", "DO001", "GDN01"));
        position.LedgerScope.Should().Be(StockLedgerScopeKeyType.Create("BRG01", "DO001"));
        position.Version.Should().Be(0);

        Action wrongLayer = () => position.AddLayer(
            Layer("LYR-X", 1m, ReceiptT1, ExpAug, ValuationA, location: Apotek));
        wrongLayer.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UT17_LayerAndPosition_DoNotEncodeAuthority()
    {
        foreach (var type in new[]
                 {
                     typeof(StockLayerModel),
                     typeof(StockPositionModel),
                     typeof(StockFifoAllocator),
                     typeof(StockAllocationResult)
                 })
        {
            type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => m.Name)
                .Should()
                .NotContain(name => name.Contains("Authoritative", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static StockLayerModel Layer(
        string stockLayerId,
        decimal quantity,
        DateTime effectiveReceiptTime,
        DateOnly? expirationDate,
        UnitValuationType valuation,
        IBrgKey? item = null,
        IReceiptSourceKey? receiptSource = null,
        ILayananKey? location = null,
        string? batch = null)
        => StockLayerModel.Create(
            item ?? Item,
            receiptSource ?? ReceiptSource,
            location ?? Gudang,
            StockMovementModel.Key("MOV-FORM-" + stockLayerId),
            quantity,
            valuation,
            effectiveReceiptTime,
            StockFactOriginEnum.Native,
            expirationDate,
            batch,
            stockLayerId);
}
