using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class ReconcileScopeQueryTest
{
    [Fact]
    public async Task Handle_ReturnsEmptyConsistentResponse_WithoutTouchingReposOrLegacyPort()
    {
        var batchRepo = new Mock<IStockBatchRepo>(MockBehavior.Strict);
        var mutasiRepo = new Mock<IStockMutasiRepo>(MockBehavior.Strict);
        var legacyRead = new Mock<ILegacyStockReadPort>(MockBehavior.Strict);
        var options = Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = false });

        var sut = new ReconcileScopeHandler(
            batchRepo.Object,
            mutasiRepo.Object,
            legacyRead.Object,
            options);

        var result = await sut.Handle(
            new ReconcileScopeQuery("BRG1", "DO1", "LYN1"),
            CancellationToken.None);

        result.BrgId.Should().Be("BRG1");
        result.BrgMasukReffId.Should().Be("DO1");
        result.LayananId.Should().Be("LYN1");
        result.IsConsistent.Should().BeTrue();
        result.BatchQtySisa.Should().Be(0);
        result.LokasiQtyTotal.Should().Be(0);
        result.MovementNet.Should().Be(0);
        result.LocationLines.Should().BeEmpty();
        result.Differences.Should().BeEmpty();
        result.LegacySnapshot.Should().BeNull();
    }
}
