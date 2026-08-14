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

    [Fact]
    public async Task EnsureFreshAsync_DelegatesMultiScope_WhenCoexistenceEnabled()
    {
        var scopes = new List<ScopeKeyDto>
        {
            new("BRG1", "DO1"),
            new("BRG2", "DO2")
        };

        EnsureFreshnessForScopesCommand? sent = null;
        _mediator
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<EnsureFreshnessForScopesResult>, CancellationToken>((req, _) =>
                sent = (EnsureFreshnessForScopesCommand)req)
            .ReturnsAsync(new EnsureFreshnessForScopesResult(
                EnsureFreshnessOutcomeEnum.Success,
                string.Empty,
                string.Empty,
                string.Empty));

        var sut = CreateGate(coexistenceEnabled: true, _mediator.Object);
        var result = await sut.EnsureFreshAsync(scopes, UserId);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.Success);
        sent.Should().NotBeNull();
        sent!.Scopes.Should().HaveCount(2);
        sent.UserId.Should().Be(UserId);
        _mediator.Verify(
            x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureFreshAsync_NoOps_WhenCoexistenceDisabled()
    {
        // ADR-STL-007 cutover: gate no-ops when StockLedger:CoexistenceEnabled=false.
        var sut = CreateGate(coexistenceEnabled: false, _mediator.Object);

        var result = await sut.EnsureFreshAsync(
            [new ScopeKeyDto("BRG1", "DO1")],
            UserId);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.Success);
        _mediator.Verify(
            x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnsureFreshAsync_PropagatesInconsistentAbort()
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnsureFreshnessForScopesResult(
                EnsureFreshnessOutcomeEnum.AbortedInconsistent,
                "scope broken",
                "BRG1",
                "DO1"));

        var sut = CreateGate(coexistenceEnabled: true, _mediator.Object);
        var result = await sut.EnsureFreshAsync(
            [new ScopeKeyDto("BRG1", "DO1")],
            UserId);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.AbortedInconsistent);
        result.InconsistencyReason.Should().Be("scope broken");
        result.FailedBrgId.Should().Be("BRG1");
        result.FailedBrgMasukReffId.Should().Be("DO1");
    }
}
