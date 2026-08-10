using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// UC-STL-021 — Get Availability At Location (minimal preview).
/// Returns QtySisa &gt; 0 candidates ordered for allocation preview; does not authorize sale.
/// </summary>
public record GetAvailabilityAtLocationQuery(
    string BrgId,
    string LayananId,
    DateTime? TglEd = null) : IRequest<GetAvailabilityAtLocationResponse>;

public record AvailabilityCandidateLine(
    int PreviewSequence,
    string StokLokasiId,
    string StokBatchId,
    string BrgMasukReffId,
    DateTime TglEd,
    DateTime TglMasuk,
    decimal QtySisa);

public record GetAvailabilityAtLocationResponse(
    string BrgId,
    string LayananId,
    DateTime? TglEdFilter,
    IReadOnlyList<AvailabilityCandidateLine> Candidates);

public class GetAvailabilityAtLocationHandler
    : IRequestHandler<GetAvailabilityAtLocationQuery, GetAvailabilityAtLocationResponse>
{
    private readonly IStockBatchRepo _batchRepo;

    public GetAvailabilityAtLocationHandler(IStockBatchRepo batchRepo) =>
        _batchRepo = batchRepo;

    public Task<GetAvailabilityAtLocationResponse> Handle(
        GetAvailabilityAtLocationQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);

        var candidates = _batchRepo
            .ListAllocationCandidates(request.BrgId, request.LayananId)
            .Select(StockAllocationCandidateType.FromLokasi)
            .Where(c => c.QtySisa > 0)
            .ToList();

        // OrderForPreview applies Explicit-ED filter when TglEd is supplied (BR-STL-023).
        var ordered = StockOutboundAllocator.OrderForPreview(candidates, request.TglEd);

        var lines = ordered
            .Select((c, i) => new AvailabilityCandidateLine(
                PreviewSequence: i + 1,
                c.StokLokasiId,
                c.StokBatchId,
                c.BrgMasukReffId,
                c.TglEd,
                c.TglMasuk,
                c.QtySisa))
            .ToList();

        return Task.FromResult(new GetAvailabilityAtLocationResponse(
            request.BrgId,
            request.LayananId,
            request.TglEd,
            lines));
    }
}
