using System.ComponentModel.DataAnnotations;
using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;

public record RecordRecountCommand(
    string SessionId,
    string VarianceItemId,
    decimal RecountQty,
    DiscrepancyReasonEnum? DiscrepancyReason,
    string Notes,
    string RecountedBy,
    DateTime? RecountTime = null) : IRequest<RecordRecountResponse>;

public record RecordRecountResponse(
    string VarianceItemId,
    decimal RecountQty,
    decimal ReconstructedSystemQty,
    decimal FinalVarianceQty,
    decimal FinalValuation,
    DiscrepancyReasonEnum DiscrepancyReason,
    string Notes,
    bool IsVerified);

public class RecordRecountHandler : IRequestHandler<RecordRecountCommand, RecordRecountResponse>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;
    private readonly ISystemQuantityReconstructionService _reconstructionService;

    public RecordRecountHandler(
        IStockOpnameSessionRepo sessionRepo,
        ISystemQuantityReconstructionService reconstructionService)
    {
        _sessionRepo = sessionRepo;
        _reconstructionService = reconstructionService;
    }

    public async Task<RecordRecountResponse> Handle(
        RecordRecountCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SessionId, nameof(request.SessionId));
        Guard.Against.NullOrWhiteSpace(request.VarianceItemId, nameof(request.VarianceItemId));
        Guard.Against.NullOrWhiteSpace(request.RecountedBy, nameof(request.RecountedBy));

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"Stock opname session with ID '{request.SessionId}' not found.");

        var session = maybe.Value;
        if (session.Status != SessionStatusEnum.Verifying)
            throw new InvalidOperationException($"Cannot record recount when session status is {session.Status}. Expected Verifying.");

        var varianceItem = session.VarianceItems.FirstOrDefault(v => v.VarianceItemId == request.VarianceItemId);
        if (varianceItem == null)
            throw new KeyNotFoundException($"Variance item with ID '{request.VarianceItemId}' not found in session '{request.SessionId}'.");

        // BR-SO-006 / TD-06: Segregation of Duties
        var hasCounted = _sessionRepo.HasTupleBeenCountedByUser(
            session.SessionId,
            varianceItem.BrgId,
            varianceItem.BatchNo,
            varianceItem.ExpiredDate,
            request.RecountedBy);

        if (hasCounted)
        {
            throw new ValidationException("Staf penghitung fisik awal dilarang melakukan verifikasi hitung ulang pada barang ini.");
        }

        // BR-SO-012: Reconstruct system quantity at recount time
        var recountTime = request.RecountTime ?? DateTime.Now;
        var reconstructedBalances = await _reconstructionService.ReconstructBalancesAsync(
            session.LayananId,
            recountTime,
            [varianceItem.BrgId],
            cancellationToken);

        var tupleKey = new ItemTupleKey(varianceItem.BrgId, varianceItem.BatchNo, varianceItem.ExpiredDate);
        var reconstructedQty = reconstructedBalances.TryGetValue(tupleKey, out var bal) ? bal : 0m;

        var finalVarianceQty = request.RecountQty - reconstructedQty;
        DiscrepancyReasonEnum reason;

        // BR-SO-014: Mandatory discrepancy reason & notes when variance persists
        if (finalVarianceQty != 0)
        {
            if (!request.DiscrepancyReason.HasValue || !Enum.IsDefined(typeof(DiscrepancyReasonEnum), request.DiscrepancyReason.Value))
            {
                throw new ValidationException("Discrepancy reason is mandatory when variance persists after recount.");
            }

            if (string.IsNullOrWhiteSpace(request.Notes))
            {
                throw new ValidationException("Investigation notes are mandatory when variance persists after recount.");
            }

            reason = request.DiscrepancyReason.Value;
        }
        else
        {
            reason = request.DiscrepancyReason ?? DiscrepancyReasonEnum.ADMIN_ERROR;
        }

        var recountResultId = !string.IsNullOrWhiteSpace(varianceItem.RecountResult?.RecountResultId)
            ? varianceItem.RecountResult.RecountResultId
            : $"REC{Guid.NewGuid():N}"[..12];

        var recountResult = StockOpnameRecountResultModel.Create(
            recountResultId: recountResultId,
            varianceItemId: varianceItem.VarianceItemId,
            recountQty: request.RecountQty,
            reconstructedSystemQty: reconstructedQty,
            hpp: varianceItem.Hpp,
            discrepancyReason: reason,
            notes: request.Notes?.Trim() ?? string.Empty,
            recountedBy: request.RecountedBy.Trim(),
            recountTime: recountTime);

        varianceItem.SetRecountResult(recountResult);
        _sessionRepo.SaveChanges(session);

        return new RecordRecountResponse(
            varianceItem.VarianceItemId,
            recountResult.RecountQty,
            recountResult.ReconstructedSystemQty,
            recountResult.FinalVarianceQty,
            recountResult.FinalValuation,
            recountResult.DiscrepancyReason,
            recountResult.Notes,
            varianceItem.IsVerified);
    }
}
