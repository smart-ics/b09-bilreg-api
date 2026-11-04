using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.BedOperationalFeature.UseCases;

public record BedOperationalVerifyReadyCommand(
    string BedId,
    string VerifiedByActorId,
    string? ResponsibleActorId = null,
    string? Reason = null,
    string? EvidenceReference = null,
    DateTime? OccurredAt = null,
    string? SourceFactId = null,
    string? RequestId = null,
    string? LayananId = null
) : IRequest<BedOperationalVerifyReadyResult>;

public record BedOperationalVerifyReadyResult(
    string BedId,
    string ReadinessStatus,
    string LatestTransactionId,
    DateTime OccurredAt
);

public class BedOperationalVerifyReadyCommandHandler
    : IRequestHandler<BedOperationalVerifyReadyCommand, BedOperationalVerifyReadyResult>
{
    private readonly IBedOperationalRepo _bedOperationalRepo;
    private readonly IBedRepo _bedRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public BedOperationalVerifyReadyCommandHandler(
        IBedOperationalRepo bedOperationalRepo,
        IBedRepo bedRepo,
        ITglJamProvider tglJamProvider)
    {
        _bedOperationalRepo = bedOperationalRepo;
        _bedRepo = bedRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<BedOperationalVerifyReadyResult> Handle(
        BedOperationalVerifyReadyCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BedId, nameof(request.BedId));
        Guard.Against.NullOrWhiteSpace(request.VerifiedByActorId, nameof(request.VerifiedByActorId));
        cancellationToken.ThrowIfCancellationRequested();

        var nowUtc = NormalizeToUtc(_tglJamProvider.Now);
        var resolvedOccurredAt = SimulationTimeGuard.Resolve(
            request.OccurredAt,
            _tglJamProvider,
            operationName: "BedOperationalVerifyReady");
        var occurredAt = NormalizeToUtc(resolvedOccurredAt);
        var recordedAt = nowUtc >= occurredAt ? nowUtc : occurredAt;

        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Verifikasi pembersihan bed selesai (HK)"
            : request.Reason;
        var evidenceRef = request.EvidenceReference ?? string.Empty;
        var responsibleActor = string.IsNullOrWhiteSpace(request.ResponsibleActorId)
            ? request.VerifiedByActorId
            : request.ResponsibleActorId;
        var sourceFactId = request.SourceFactId ?? string.Empty;
        var requestId = string.IsNullOrWhiteSpace(request.RequestId)
            ? Guid.NewGuid().ToString()
            : request.RequestId;

        var maybeBedOp = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(request.BedId));
        BedOperationalModel bedOp;
        if (maybeBedOp.HasValue)
        {
            bedOp = maybeBedOp.Value;
        }
        else
        {
            var bed = _bedRepo.LoadEntity(BedType.Key(request.BedId))
                .GetValueOrThrow($"Bed '{request.BedId}' tidak ditemukan.");
            bedOp = BedOperationalModel.Create(
                bed.BedId,
                bed.Bangsal.BangsalId,
                bed.Kamar.KamarId,
                OccupancyPolicyReff.Default);
        }

        // Pastikan occurredAt transaksi verifikasi ready tidak lebih lampau daripada transaksi terakhir
        var latestTx = bedOp.ListReadinessTransaction
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.TransactionId, StringComparer.Ordinal)
            .LastOrDefault();

        if (latestTx != null && occurredAt <= latestTx.OccurredAt)
        {
            occurredAt = latestTx.OccurredAt.AddSeconds(1);
        }
        if (recordedAt < occurredAt)
        {
            recordedAt = occurredAt;
        }

        var updated = bedOp.VerifyReady(
            reason,
            evidenceRef,
            responsibleActor,
            request.VerifiedByActorId,
            occurredAt,
            recordedAt,
            sourceFactId,
            requestId);

        _bedOperationalRepo.SaveChanges(updated);

        var result = new BedOperationalVerifyReadyResult(
            updated.BedId,
            updated.CurrentReadiness?.ToString() ?? BedReadinessStatusEnum.Ready.ToString(),
            updated.LatestReadinessTransactionId,
            occurredAt);

        return Task.FromResult(result);
    }

    private static DateTime NormalizeToUtc(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc)
            return dt;
        if (dt.Kind == DateTimeKind.Local)
            return dt.ToUniversalTime();
        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }
}
