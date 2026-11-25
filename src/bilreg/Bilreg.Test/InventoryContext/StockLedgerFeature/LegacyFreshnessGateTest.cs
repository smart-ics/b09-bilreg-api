using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class LegacyFreshnessGateTest
{
    private readonly Mock<IMediator> _mediator = new();
    private const string UserId = "U1";

    private static LegacyFreshnessGate CreateGate(bool coexistenceEnabled, IMediator mediator)
    {
        var options = Options.Create(new StockLedgerCoexistenceOptions
        {
            CoexistenceEnabled = coexistenceEnabled
        });
        return new LegacyFreshnessGate(mediator, options);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EnsureFreshAsync_ReturnsSuccessNoOp_WithoutCallingMediator(bool coexistenceEnabled)
    {
        var scopes = new List<ScopeKeyDto>
        {
            new("BRG1", "DO1"),
            new("BRG2", "DO2")
        };

        var sut = CreateGate(coexistenceEnabled, _mediator.Object);
        var result = await sut.EnsureFreshAsync(scopes, UserId);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.Success);
        result.InconsistencyReason.Should().BeEmpty();
        result.FailedBrgId.Should().BeEmpty();
        result.FailedBrgMasukReffId.Should().BeEmpty();
        _mediator.Verify(
            x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
