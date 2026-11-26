using System.ComponentModel.DataAnnotations;
using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;

public record BulkVerifyMinorVariancesCommand(
    string SessionId,
    decimal MaxNominalThreshold,
    DiscrepancyReasonEnum StandardReason,
    string Notes,
    string VerifiedBy,
    DateTime? VerifiedAt = null) : IRequest<BulkVerifyMinorVariancesResponse>;

public record BulkVerifyMinorVariancesResponse(
    string SessionId,
    int VerifiedCount,
    IReadOnlyList<string> VerifiedVarianceItemIds);

public class BulkVerifyMinorVariancesHandler
    : IRequestHandler<BulkVerifyMinorVariancesCommand, BulkVerifyMinorVariancesResponse>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;

    public BulkVerifyMinorVariancesHandler(IStockOpnameSessionRepo sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public Task<BulkVerifyMinorVariancesResponse> Handle(
        BulkVerifyMinorVariancesCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SessionId, nameof(request.SessionId));
        Guard.Against.NullOrWhiteSpace(request.VerifiedBy, nameof(request.VerifiedBy));

        if (request.MaxNominalThreshold < 0)
            throw new ArgumentException("Threshold must be non-negative.", nameof(request.MaxNominalThreshold));

        if (!Enum.IsDefined(typeof(DiscrepancyReasonEnum), request.StandardReason))
            throw new ValidationException("Standard discrepancy reason is invalid.");

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"Stock opname session with ID '{request.SessionId}' not found.");

        var session = maybe.Value;
        if (session.Status != SessionStatusEnum.Verifying)
            throw new InvalidOperationException($"Cannot verify variances when session status is {session.Status}. Expected Verifying.");

        var verifiedAt = request.VerifiedAt ?? DateTime.Now;
        var verifiedIds = new List<string>();

        var candidates = session.VarianceItems
            .Where(v => !v.IsVerified && Math.Abs(v.VarianceValuation) <= request.MaxNominalThreshold)
            .ToList();

        foreach (var v in candidates)
        {
            var recountResultId = !string.IsNullOrWhiteSpace(v.RecountResult?.RecountResultId)
                ? v.RecountResult.RecountResultId
                : $"REC{Guid.NewGuid():N}"[..12];

            v.MarkVerifiedWithReason(
                request.StandardReason,
                request.Notes ?? string.Empty,
                request.VerifiedBy.Trim(),
                verifiedAt,
                recountResultId);

            verifiedIds.Add(v.VarianceItemId);
        }

        if (verifiedIds.Count > 0)
        {
            _sessionRepo.SaveChanges(session);
        }

        return Task.FromResult(new BulkVerifyMinorVariancesResponse(
            session.SessionId,
            verifiedIds.Count,
            verifiedIds.AsReadOnly()));
    }
}
