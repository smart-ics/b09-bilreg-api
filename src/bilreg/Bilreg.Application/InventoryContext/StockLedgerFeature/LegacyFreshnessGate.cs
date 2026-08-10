using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using MediatR;
using Microsoft.Extensions.Options;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Application helper for native posts (S1-D+): invoke UC-STL-012 before UoW (architecture §13).
/// </summary>
public sealed class LegacyFreshnessGate
{
    private readonly IMediator _mediator;
    private readonly StockLedgerCoexistenceOptions _options;

    public LegacyFreshnessGate(
        IMediator mediator,
        IOptions<StockLedgerCoexistenceOptions> options)
    {
        _mediator = mediator;
        _options = options.Value;
    }

    public Task<EnsureFreshnessForScopesResult> EnsureFreshAsync(
        IReadOnlyList<ScopeKeyDto> scopes,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // ADR-STL-007: when CoexistenceEnabled=false (cutover), gate no-ops; legacy ports unused.
        // S1 tests primarily cover CoexistenceEnabled=true.
        if (!_options.CoexistenceEnabled)
        {
            return Task.FromResult(new EnsureFreshnessForScopesResult(
                EnsureFreshnessOutcomeEnum.Success,
                string.Empty,
                string.Empty,
                string.Empty));
        }

        return _mediator.Send(
            new EnsureFreshnessForScopesCommand(scopes, userId),
            cancellationToken);
    }
}
