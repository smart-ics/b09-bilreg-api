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
        // Deactivated (P1-S02): legacy tables are the system of record; no-op fresh result.
        return Task.FromResult(new EnsureFreshnessForScopesResult(
            EnsureFreshnessOutcomeEnum.Success,
            string.Empty,
            string.Empty,
            string.Empty));
    }
}
