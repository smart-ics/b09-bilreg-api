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

    public async Task<EnsureFreshnessForScopesResult> Handle(
        EnsureFreshnessForScopesCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request.Scopes);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        foreach (var key in request.Scopes)
        {
            Guard.Against.NullOrWhiteSpace(key.BrgId);
            Guard.Against.NullOrWhiteSpace(key.BrgMasukReffId);

            var scopeKey = StockLegacyScopeModel.Key(key.BrgId, key.BrgMasukReffId);
            var scopeMaybe = _scopeRepo.LoadEntity(scopeKey);
            var scope = scopeMaybe.HasValue
                ? scopeMaybe.Value
                : StockLegacyScopeModel.CreateNotAligned(key.BrgId, key.BrgMasukReffId);

            if (scope.AlignmentStatus == AlignmentStatusEnum.Inconsistent)
            {
                return Abort(key, scope.InconsistencyReason);
            }

            if (scope.AlignmentStatus == AlignmentStatusEnum.NotAligned)
            {
                var hydrate = await _mediator.Send(
                    new HydrateScopeFromLegacyCommand(key.BrgId, key.BrgMasukReffId, request.UserId),
                    cancellationToken);

                if (hydrate.Outcome == HydrateScopeOutcomeEnum.Inconsistent)
                    return Abort(key, hydrate.InconsistencyReason);
            }
            else if (scope.AlignmentStatus == AlignmentStatusEnum.Aligned)
            {
                var journals = _legacyRead.ListJournals(key.BrgId, key.BrgMasukReffId);
                if (LegacyWatermarkHelper.HasLegacyRowsBeyondWatermark(journals, scope))
                {
                    scope.MarkStale();
                    _scopeRepo.SaveChanges(scope);
                }
            }

            var catchUp = await _mediator.Send(
                new CatchUpScopeFromLegacyCommand(key.BrgId, key.BrgMasukReffId, request.UserId),
                cancellationToken);

            if (catchUp.Outcome == CatchUpScopeOutcomeEnum.Inconsistent)
                return Abort(key, catchUp.InconsistencyReason);

            if (catchUp.Outcome == CatchUpScopeOutcomeEnum.NotAligned)
            {
                return Abort(
                    key,
                    catchUp.InconsistencyReason.Length > 0
                        ? catchUp.InconsistencyReason
                        : "Catch-up reported NotAligned after hydrate.");
            }
        }

        return new EnsureFreshnessForScopesResult(
            EnsureFreshnessOutcomeEnum.Success,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static EnsureFreshnessForScopesResult Abort(ScopeKeyDto key, string reason) =>
        new(
            EnsureFreshnessOutcomeEnum.AbortedInconsistent,
            reason,
            key.BrgId,
            key.BrgMasukReffId);
}
