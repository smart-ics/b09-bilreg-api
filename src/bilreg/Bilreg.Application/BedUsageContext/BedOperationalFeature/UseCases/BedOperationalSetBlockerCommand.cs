using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.BedOperationalFeature.UseCases;

public record BedOperationalSetBlockerCommand(
    string BedId,
    string ActorId,
    string Reason,
    BedRestrictionTypeEnum RestrictionType = BedRestrictionTypeEnum.Maintenance,
    bool IsBlocked = true,
    string? EvidenceReference = null,
    DateTime? OccurredAt = null,
    string? SourceFactId = null,
    string? RequestId = null
) : IRequest<BedOperationalSetBlockerResult>;

public record BedOperationalSetBlockerResult(
    string BedId,
    string ReadinessStatus,
    bool IsBlocked,
    string LatestTransactionId,
    DateTime OccurredAt
);

public class BedOperationalSetBlockerCommandHandler
    : IRequestHandler<BedOperationalSetBlockerCommand, BedOperationalSetBlockerResult>
{
    private readonly IBedOperationalRepo _bedOperationalRepo;
    private readonly IBedRepo _bedRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public BedOperationalSetBlockerCommandHandler(
        IBedOperationalRepo bedOperationalRepo,
        IBedRepo bedRepo,
        ITglJamProvider tglJamProvider)
    {
        _bedOperationalRepo = bedOperationalRepo;
        _bedRepo = bedRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<BedOperationalSetBlockerResult> Handle(
        BedOperationalSetBlockerCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BedId, nameof(request.BedId));
        Guard.Against.NullOrWhiteSpace(request.ActorId, nameof(request.ActorId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        cancellationToken.ThrowIfCancellationRequested();

        var nowUtc = NormalizeToUtc(_tglJamProvider.Now);
        var occurredAt = request.OccurredAt.HasValue
            ? NormalizeToUtc(request.OccurredAt.Value)
            : nowUtc;
        var recordedAt = nowUtc >= occurredAt ? nowUtc : occurredAt;

        var evidenceRef = request.EvidenceReference ?? string.Empty;
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

        BedOperationalModel updated;
        if (request.IsBlocked)
        {
            var status = request.RestrictionType == BedRestrictionTypeEnum.Cleaning
                ? BedReadinessStatusEnum.CleaningRequired
                : BedReadinessStatusEnum.Blocked;

            updated = bedOp.RecordReadiness(
                status,
                request.RestrictionType,
                request.Reason,
                evidenceRef,
                request.ActorId,
                occurredAt,
                recordedAt,
                sourceFactId,
                requestId);
        }
        else
        {
            updated = bedOp.VerifyReady(
                request.Reason,
                evidenceRef,
                request.ActorId,
                request.ActorId,
                occurredAt,
                recordedAt,
                sourceFactId,
                requestId);
        }

        _bedOperationalRepo.SaveChanges(updated);

        var result = new BedOperationalSetBlockerResult(
            updated.BedId,
            updated.CurrentReadiness?.ToString() ?? BedReadinessStatusEnum.Ready.ToString(),
            updated.HasUnresolvedBlocker,
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
