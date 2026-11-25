using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Queries;

public record StockOpnameResultReportItemDto(
    string VarianceItemId,
    string SessionId,
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    string BaseSatuanId,
    decimal CountQty,
    decimal SystemQty,
    decimal InitialVarianceQty,
    decimal? RecountQty,
    decimal FinalVarianceQty,
    decimal Hpp,
    decimal FinalValuation,
    DiscrepancyReasonEnum? DiscrepancyReason,
    string? DiscrepancyReasonName,
    string Notes,
    string RecountedBy,
    DateTime? RecountTime,
    bool IsVerified,
    string VerificationStatus
);

public record StockOpnameResultReportDto(
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
    decimal TotalSurplusValuation,
    decimal TotalDeficitValuation,
    decimal NetDiscrepancyValuation,
    int TotalVarianceItems,
    int TotalVerifiedItems,
    int TotalUnverifiedItems,
    bool AllVerified,
    IReadOnlyList<StockOpnameResultReportItemDto> Items
);

public record GetSessionResultReportQuery(string SessionId) : IRequest<StockOpnameResultReportDto?>;

public class GetSessionResultReportHandler : IRequestHandler<GetSessionResultReportQuery, StockOpnameResultReportDto?>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;

    public GetSessionResultReportHandler(IStockOpnameSessionRepo sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public Task<StockOpnameResultReportDto?> Handle(
        GetSessionResultReportQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
            return Task.FromResult<StockOpnameResultReportDto?>(null);

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            return Task.FromResult<StockOpnameResultReportDto?>(null);

        var session = maybe.Value;
        var items = session.VarianceItems.Select(v => new StockOpnameResultReportItemDto(
            v.VarianceItemId,
            v.SessionId,
            v.BrgId,
            v.BatchNo,
            v.ExpiredDate,
            v.BaseSatuanId,
            v.CountQty,
            v.SystemQty,
            v.VarianceQty,
            v.RecountResult?.RecountQty,
            v.FinalVarianceQty,
            v.Hpp,
            v.FinalValuation,
            v.RecountResult?.DiscrepancyReason,
            v.RecountResult?.DiscrepancyReason.ToString(),
            v.RecountResult?.Notes ?? string.Empty,
            v.RecountResult?.RecountedBy ?? string.Empty,
            v.RecountResult?.RecountTime,
            v.IsVerified,
            v.IsVerified ? "Verified" : "Unverified"
        )).ToList();

        var totalVarianceItems = items.Count;
        var totalVerifiedItems = items.Count(i => i.IsVerified);
        var totalUnverifiedItems = totalVarianceItems - totalVerifiedItems;
        var allVerified = totalVarianceItems == 0 || totalUnverifiedItems == 0;

        var report = new StockOpnameResultReportDto(
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
            session.SurplusValuation,
            session.DeficitValuation,
            session.NetValuation,
            totalVarianceItems,
            totalVerifiedItems,
            totalUnverifiedItems,
            allVerified,
            items
        );

        return Task.FromResult<StockOpnameResultReportDto?>(report);
    }
}
