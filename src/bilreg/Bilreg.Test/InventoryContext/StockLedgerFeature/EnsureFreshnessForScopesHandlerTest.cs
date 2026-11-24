using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using FluentAssertions;
using MediatR;
using Moq;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class EnsureFreshnessForScopesHandlerTest
{
    [Fact]
    public async Task Handle_ReturnsSuccessNoOp_WithoutTouchingReposOrMediator()
    {
        var legacyRead = new Mock<ILegacyStockReadPort>(MockBehavior.Strict);
        var scopeRepo = new Mock<IStockLegacyScopeRepo>(MockBehavior.Strict);
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var sut = new EnsureFreshnessForScopesHandler(
            legacyRead.Object,
            scopeRepo.Object,
            mediator.Object);

        var result = await sut.Handle(
            new EnsureFreshnessForScopesCommand([new ScopeKeyDto("BRG1", "DO1")], "U1"),
            CancellationToken.None);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.Success);
        result.InconsistencyReason.Should().BeEmpty();
        result.FailedBrgId.Should().BeEmpty();
        result.FailedBrgMasukReffId.Should().BeEmpty();
    }
}
