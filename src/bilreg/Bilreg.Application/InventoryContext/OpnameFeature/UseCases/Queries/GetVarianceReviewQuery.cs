using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Queries;

public record StockOpnameVarianceReviewDto(
    string VarianceItemId,
    string SessionId,
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    string BaseSatuanId,
    decimal PhysicalQty,
    decimal InitialSystemQty,
    decimal InitialVarianceQty,
    decimal? RecountQty,
    decimal FinalVarianceQty,
    decimal Hpp,
    decimal FinancialValue,
    DiscrepancyReasonEnum? DiscrepancyReason,
    string? DiscrepancyReasonName,
    string Notes,
    string RecountedBy,
    DateTime? RecountTime,
    int State,
    bool IsVerified
);

public record GetVarianceReviewQuery(string SessionId) : IRequest<IEnumerable<StockOpnameVarianceReviewDto>>;

public class GetVarianceReviewHandler : IRequestHandler<GetVarianceReviewQuery, IEnumerable<StockOpnameVarianceReviewDto>>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;

    public GetVarianceReviewHandler(IStockOpnameSessionRepo sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public Task<IEnumerable<StockOpnameVarianceReviewDto>> Handle(
        GetVarianceReviewQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
            return Task.FromResult<IEnumerable<StockOpnameVarianceReviewDto>>([]);

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            return Task.FromResult<IEnumerable<StockOpnameVarianceReviewDto>>([]);

        var session = maybe.Value;
        var result = session.VarianceItems.Select(v => new StockOpnameVarianceReviewDto(
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
            v.State,
            v.IsVerified
        )).ToList();

        return Task.FromResult<IEnumerable<StockOpnameVarianceReviewDto>>(result);
    }
}
