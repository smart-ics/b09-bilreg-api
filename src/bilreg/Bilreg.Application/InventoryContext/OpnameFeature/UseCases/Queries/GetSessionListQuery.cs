using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Queries;

public record StockOpnameSessionListDto(
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
    string RevisionNotes
);

public record GetSessionListQuery(
    string? LayananId = null,
    SessionStatusEnum? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<IEnumerable<StockOpnameSessionListDto>>;

public class GetSessionListHandler : IRequestHandler<GetSessionListQuery, IEnumerable<StockOpnameSessionListDto>>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;

    public GetSessionListHandler(IStockOpnameSessionRepo sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public Task<IEnumerable<StockOpnameSessionListDto>> Handle(
        GetSessionListQuery request,
        CancellationToken cancellationToken)
    {
        var sessions = _sessionRepo.ListData(
            request.LayananId,
            request.Status,
            request.FromDate,
            request.ToDate);

        var result = sessions.Select(s => new StockOpnameSessionListDto(
            s.SessionId,
            s.LayananId,
            s.ScopeMode,
            s.Status,
            s.CutoffDate,
            s.CreatedBy,
            s.CreatedAt,
            s.CountingSubmittedBy,
            s.CountingSubmittedAt,
            s.VerifiedBy,
            s.VerifiedAt,
            s.ApprovedBy,
            s.ApprovedAt,
            s.RejectedBy,
            s.RejectedAt,
            s.CancelledBy,
            s.CancelledAt,
            s.CancelReason,
            s.RevisionNotes
        )).ToList();

        return Task.FromResult<IEnumerable<StockOpnameSessionListDto>>(result);
    }
}
