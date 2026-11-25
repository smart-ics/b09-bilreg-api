using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

public record ScopeKeyDto(string BrgId, string BrgMasukReffId);

public record EnsureFreshnessForScopesCommand(
    IReadOnlyList<ScopeKeyDto> Scopes,
    string UserId) : IRequest<EnsureFreshnessForScopesResult>;

public enum EnsureFreshnessOutcomeEnum
{
    Success = 0,
    AbortedInconsistent = 1
}

public record EnsureFreshnessForScopesResult(
    EnsureFreshnessOutcomeEnum Outcome,
    string InconsistencyReason,
    string FailedBrgId,
    string FailedBrgMasukReffId);

/// <summary>
/// UC-STL-012 — gate-on-demand freshness (GAP-STL-004): hydrate if NotAligned, catch-up if Stale/Aligned with lag;
/// abort when Inconsistent. No background worker.
/// </summary>
public class EnsureFreshnessForScopesHandler
    : IRequestHandler<EnsureFreshnessForScopesCommand, EnsureFreshnessForScopesResult>
{
    private readonly ILegacyStockReadPort _legacyRead;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly IMediator _mediator;

    public EnsureFreshnessForScopesHandler(
        ILegacyStockReadPort legacyRead,
        IStockLegacyScopeRepo scopeRepo,
        IMediator mediator)
    {
        _legacyRead = legacyRead;
        _scopeRepo = scopeRepo;
        _mediator = mediator;
    }

    public Task<EnsureFreshnessForScopesResult> Handle(
        EnsureFreshnessForScopesCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request.Scopes);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        // Deactivated (P1-S02): legacy tables are the system of record; no-op fresh result.
        return Task.FromResult(new EnsureFreshnessForScopesResult(
            EnsureFreshnessOutcomeEnum.Success,
            string.Empty,
            string.Empty,
            string.Empty));
    }

    private static EnsureFreshnessForScopesResult Abort(ScopeKeyDto key, string reason) =>
        new(
            EnsureFreshnessOutcomeEnum.AbortedInconsistent,
            reason,
            key.BrgId,
            key.BrgMasukReffId);
}
