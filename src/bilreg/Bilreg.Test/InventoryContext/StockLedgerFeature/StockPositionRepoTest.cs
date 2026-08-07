using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockPositionRepoTest
{
    private readonly StockPositionRepo _sut = new(
        new StockPositionDal(ConnStringHelper.GetTestEnv()),
        new StockLayerDal(ConnStringHelper.GetTestEnv()));

    private static readonly IBrgKey Item = new BrgReff("BRGS602", "Item Pos");
    private static readonly IReceiptSourceKey ReceiptSource = ReceiptSourceType.Key("DO6002");
    private static readonly ILayananKey Location = LayananType.Key("GD602");
    private static readonly DateTime ReceiptTime = new(2026, 8, 7, 9, 0, 0);

    [Fact]
    public void SaveAndLoad_PositionWithLayer_RoundTrips()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var position = StockPositionModel.CreateEmpty(Item, ReceiptSource, Location)
            .AddLayer(BuildLayer("LYRS602A", 10m));

        _sut.SaveChanges(position);

        var loaded = _sut.LoadEntity(position);
        loaded.HasValue.Should().BeTrue();
        loaded.Value.Version.Should().Be(1);
        loaded.Value.Layers.Should().ContainSingle();
        loaded.Value.Layers[0].StockLayerId.Should().Be("LYRS602A");
        loaded.Value.Layers[0].RemainingQuantity.Should().Be(10m);
        loaded.Value.TotalRemainingQuantity.Should().Be(10m);
    }

    [Fact]
    public void SaveAndLoad_DepletedLayer_IsRetainedAtZero()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var position = StockPositionModel.CreateEmpty(Item, ReceiptSource, Location)
            .AddLayer(BuildLayer("LYRS602B", 5m));
        _sut.SaveChanges(position);

        var stored = _sut.LoadEntity(position).Value;
        var (result, depleted) = stored.Allocate(5m);
        result.IsFulfilled.Should().BeTrue();
        depleted.Layers.Should().ContainSingle(x => x.RemainingQuantity == 0m);

        _sut.SaveChanges(depleted);

        var reloaded = _sut.LoadEntity(position).Value;
        reloaded.Version.Should().Be(2);
        reloaded.Layers.Should().ContainSingle();
        reloaded.Layers[0].RemainingQuantity.Should().Be(0m);
        reloaded.Layers[0].IsDepleted.Should().BeTrue();
        reloaded.TotalRemainingQuantity.Should().Be(0m);
    }

    [Fact]
    public void SaveChanges_StaleVersion_ThrowsConcurrencyConflict()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var seed = StockPositionModel.CreateEmpty(Item, ReceiptSource, Location)
            .AddLayer(BuildLayer("LYRS602C", 8m));
        _sut.SaveChanges(seed);

        var snapshotA = _sut.LoadEntity(seed).Value;
        var snapshotB = _sut.LoadEntity(seed).Value;

        var nextA = snapshotA.AddLayer(BuildLayer("LYRS602C2", 3m));
        var nextB = snapshotB.AddLayer(BuildLayer("LYRS602C3", 4m));

        _sut.SaveChanges(nextA);

        var act = () => _sut.SaveChanges(nextB);
        act.Should().Throw<StockLedgerPersistenceException>()
            .Which.Code.Should().Be("CONCURRENCY_CONFLICT");

        var winner = _sut.LoadEntity(seed).Value;
        winner.Version.Should().Be(2);
        winner.Layers.Select(x => x.StockLayerId).Should().BeEquivalentTo(["LYRS602C", "LYRS602C2"]);
    }

    private static StockLayerModel BuildLayer(string layerId, decimal qty)
        => StockLayerModel.Create(
            Item,
            ReceiptSource,
            Location,
            StockMovementModel.Key("MOVS602"),
            qty,
            UnitValuationType.Create(500m),
            ReceiptTime,
            StockFactOriginEnum.Native,
            expirationDate: new DateOnly(2027, 1, 31),
            batch: "BATCH-A",
            stockLayerId: layerId);
}
