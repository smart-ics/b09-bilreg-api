using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Queries;

public record StockOpnameScanProgressDto(
    int TotalCounted,
    int TotalMatched,
    int TotalVariance,
    decimal ProgressPercentage
);

public record StockOpnameScopeItemResponseDto(
    string ScopeItemId,
    string SessionId,
    string BrgId,
    string BrgName
);

public record StockOpnameSessionDetailDto(
    string SessionId,
    string LayananId,
    SessionScopeEnum ScopeMode,
    SessionStatusEnum Status,
    DateTime CutoffDate,
    string CreatedBy,
    DateTime CreatedAt,
    string CountingSubmittedBy,
    DateTime? CountingSubmittedAt,
    string VerifiedBy,
    DateTime? VerifiedAt,
    string ApprovedBy,
    DateTime? ApprovedAt,
    string RejectedBy,
    DateTime? RejectedAt,
    string CancelledBy,
    DateTime? CancelledAt,
    string CancelReason,
    string RevisionNotes,
    StockOpnameScanProgressDto ScanProgress,
    IReadOnlyList<StockOpnameScopeItemResponseDto> ScopeItems
);

public record GetSessionDetailQuery(string SessionId) : IRequest<StockOpnameSessionDetailDto?>;

public class GetSessionDetailHandler : IRequestHandler<GetSessionDetailQuery, StockOpnameSessionDetailDto?>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;

    public GetSessionDetailHandler(IStockOpnameSessionRepo sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public Task<StockOpnameSessionDetailDto?> Handle(
        GetSessionDetailQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
            return Task.FromResult<StockOpnameSessionDetailDto?>(null);

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            return Task.FromResult<StockOpnameSessionDetailDto?>(null);

        var session = maybe.Value;
        var totalCounted = session.CountedItems.Count;
        var totalVariance = session.VarianceItems.Count(v => v.VarianceQty != 0);
        var totalMatched = Math.Max(0, totalCounted - totalVariance);

        decimal progressPercentage;
        if (session.ScopeMode == SessionScopeEnum.Partial && session.ScopeItems.Count > 0)
        {
            progressPercentage = Math.Min(100m, Math.Round((decimal)totalCounted / session.ScopeItems.Count * 100m, 2));
        }
        else if (session.Status >= SessionStatusEnum.WaitingVerification)
        {
            progressPercentage = 100m;
        }
        else if (session.Status == SessionStatusEnum.Counting)
        {
            progressPercentage = totalCounted > 0 ? 50m : 0m;
        }
        else
        {
            progressPercentage = 0m;
        }

        var progress = new StockOpnameScanProgressDto(
            totalCounted,
            totalMatched,
            totalVariance,
            progressPercentage);

        var scopeItems = session.ScopeItems
            .Select(s => new StockOpnameScopeItemResponseDto(s.ScopeItemId, s.SessionId, s.BrgId, s.BrgName))
            .ToList();

        var detail = new StockOpnameSessionDetailDto(
            session.SessionId,
            session.LayananId,
            session.ScopeMode,
            session.Status,
            session.CutoffDate,
            session.CreatedBy,
            session.CreatedAt,
            session.CountingSubmittedBy,
            session.CountingSubmittedAt,
            session.VerifiedBy,
            session.VerifiedAt,
            session.ApprovedBy,
            session.ApprovedAt,
            session.RejectedBy,
            session.RejectedAt,
            session.CancelledBy,
            session.CancelledAt,
            session.CancelReason,
            session.RevisionNotes,
            progress,
            scopeItems
        );

        return Task.FromResult<StockOpnameSessionDetailDto?>(detail);
    }
}
