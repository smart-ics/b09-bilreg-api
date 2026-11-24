using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
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
    private readonly ILegacyStockReadPort _legacyRead;

    public GetAvailabilityAtLocationHandler(ILegacyStockReadPort legacyRead) =>
        _legacyRead = legacyRead;

    public Task<GetAvailabilityAtLocationResponse> Handle(
        GetAvailabilityAtLocationQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);

        var candidates = _legacyRead
            .ListBalancesAtLocation(request.BrgId, request.LayananId)
            .Where(b => b.QtySisa > 0)
            .Select(b => new StockAllocationCandidateType(
                b.BrgId,
                b.LayananId,
                b.LegacyStokId,
                b.LegacyStokId,
                b.BrgMasukReffId,
                b.TglEd,
                b.TglMasuk,
                b.QtySisa))
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
