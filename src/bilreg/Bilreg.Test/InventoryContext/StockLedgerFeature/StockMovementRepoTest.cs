using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockMovementRepoTest
{
    private readonly StockMovementRepo _sut = new(
        new StockMovementDal(ConnStringHelper.GetTestEnv()),
        new StockMovementLineDal(ConnStringHelper.GetTestEnv()));

    private static readonly DateTime BusinessTime = new(2026, 8, 7, 10, 0, 0);

    [Fact]
    public void SaveAndLoad_ReceiptMovement_RoundTripsLinesAndOrigin()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var movement = BuildReceipt();
        _sut.SaveChanges(movement);

        var loaded = _sut.LoadEntity(movement);
        loaded.HasValue.Should().BeTrue();
        var actual = loaded.Value;

        actual.StockMovementId.Should().Be(movement.StockMovementId);
        actual.SourceTransactionId.Should().Be(movement.SourceTransactionId);
        actual.MovementKind.Should().Be(StockMovementKindEnum.Receipt);
        actual.EffectiveBusinessTime.Should().Be(BusinessTime);
        actual.Origin.Should().Be(StockFactOriginEnum.Native);
        actual.ReversedMovementId.Should().BeNull();
        actual.CorrectedMovementId.Should().BeNull();
        actual.Lines.Should().HaveCount(1);
        actual.Lines[0].LineNo.Should().Be(1);
        actual.Lines[0].BrgId.Should().Be("BRGS601");
        actual.Lines[0].ReceiptSourceId.Should().Be("DO6001");
        actual.Lines[0].LayananId.Should().Be("GD601");
        actual.Lines[0].Direction.Should().Be(StockMovementDirectionEnum.Inbound);
        actual.Lines[0].Quantity.Should().Be(12.5m);
        actual.Lines[0].UnitValuation.AmountPerUnit.Should().Be(1000m);
        actual.Lines[0].StockLayerId.Should().Be("LYRS601");
    }

    [Fact]
    public void SaveChanges_WhenAlreadyPersisted_ThrowsImmutable()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var movement = BuildReceipt();
        _sut.SaveChanges(movement);

        var act = () => _sut.SaveChanges(movement);
        act.Should().Throw<StockLedgerPersistenceException>()
            .Which.Code.Should().Be("IMMUTABLE_CONFLICT");
    }

    [Fact]
    public void LoadEntity_WhenMissing_ReturnsNone()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var loaded = _sut.LoadEntity(StockMovementModel.Key("MOV_MISSING_P1S6"));
        loaded.HasValue.Should().BeFalse();
    }

    private static StockMovementModel BuildReceipt()
    {
        var line = StockMovementLineType.Create(
            1,
            new BrgReff("BRGS601", "Item S6"),
            ReceiptSourceType.Key("DO6001"),
            LayananType.Key("GD601"),
            StockMovementDirectionEnum.Inbound,
            12.5m,
            UnitValuationType.Create(1000m),
            StockFactOriginEnum.Native,
            StockLayerModel.Key("LYRS601"));

        return StockMovementModel.CreateReceipt(
            SourceTransactionReferenceType.Key("SRC-S6-MOV-001"),
            BusinessTime,
            [line],
            StockFactOriginEnum.Native);
    }
}
