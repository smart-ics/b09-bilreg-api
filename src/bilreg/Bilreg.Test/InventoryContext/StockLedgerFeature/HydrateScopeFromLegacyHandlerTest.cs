using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class HydrateScopeFromLegacyHandlerTest
{
    [Fact]
    public async Task Handle_ReturnsIdempotentAligned_WithoutTouchingReposOrLegacyPort()
    {
        var legacyRead = new Mock<ILegacyStockReadPort>(MockBehavior.Strict);
        var batchRepo = new Mock<IStockBatchRepo>(MockBehavior.Strict);
        var scopeRepo = new Mock<IStockLegacyScopeRepo>(MockBehavior.Strict);
        var mutasiRepo = new Mock<IStockMutasiRepo>(MockBehavior.Strict);
        var bindingRepo = new Mock<IStockLegacyBindingRepo>(MockBehavior.Strict);
        var replayer = new LegacyScopeJournalReplayer(
            batchRepo.Object,
            mutasiRepo.Object,
            bindingRepo.Object,
            scopeRepo.Object);

        var sut = new HydrateScopeFromLegacyHandler(
            legacyRead.Object,
            batchRepo.Object,
            scopeRepo.Object,
            replayer);

        var result = await sut.Handle(
            new HydrateScopeFromLegacyCommand("BRG1", "DO1", "U1"),
            CancellationToken.None);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Idempotent);
        result.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        result.InconsistencyReason.Should().BeEmpty();
        result.AppliedJournalCount.Should().Be(0);
    }
}
