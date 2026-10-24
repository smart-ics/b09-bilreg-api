using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.WardCockpitFeature;

public record WardRoomingInCommand(
    string MotherPakaiBedId,
    string BabyRegId,
    string BedId,
    string UserId,
    DateTime? TglJamIn = null,
    string? Catatan = null
) : IRequest<WardRoomingInResult>;

public record WardRoomingInResult(
    string MotherPakaiBedId,
    string BabyRegId,
    string BedId,
    DateTime TglJamIn,
    string Status
);

public class WardRoomingInCommandHandler : IRequestHandler<WardRoomingInCommand, WardRoomingInResult>
{
    private readonly IPakaiBedRepo _pakaiBedRepo;
    private readonly IBedRepo _bedRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public WardRoomingInCommandHandler(
        IPakaiBedRepo pakaiBedRepo,
        IBedRepo bedRepo,
        ITglJamProvider tglJamProvider)
    {
        _pakaiBedRepo = pakaiBedRepo;
        _bedRepo = bedRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<WardRoomingInResult> Handle(WardRoomingInCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.MotherPakaiBedId, nameof(request.MotherPakaiBedId));
        Guard.Against.NullOrWhiteSpace(request.BabyRegId, nameof(request.BabyRegId));
        Guard.Against.NullOrWhiteSpace(request.BedId, nameof(request.BedId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        cancellationToken.ThrowIfCancellationRequested();

        var motherPakaiBed = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(request.MotherPakaiBedId))
            .GetValueOrThrow($"Pakai Bed ibu '{request.MotherPakaiBedId}' tidak ditemukan.");

        if (motherPakaiBed.Periode.Keluar.Timestamp != new DateTime(3000, 1, 1) || motherPakaiBed.AuditTrail.IsVoided)
            throw new InvalidOperationException($"Pakai Bed ibu '{request.MotherPakaiBedId}' tidak dalam status aktif.");

        var bed = _bedRepo.LoadEntity(BedType.Key(request.BedId))
            .GetValueOrThrow($"Bed '{request.BedId}' tidak ditemukan.");

        var occurredAt = request.TglJamIn ?? _tglJamProvider.Now;

        var result = new WardRoomingInResult(
            request.MotherPakaiBedId,
            request.BabyRegId,
            bed.BedId,
            occurredAt,
            "Active"
        );

        return Task.FromResult(result);
    }
}
