using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.Shared.Helpers;
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
    string Status,
    string? BabyWaitingListBangsalId = null
);

public class WardRoomingInCommandHandler : IRequestHandler<WardRoomingInCommand, WardRoomingInResult>
{
    private readonly IPakaiBedRepo _pakaiBedRepo;
    private readonly IBedRepo _bedRepo;
    private readonly IWardCockpitDal _wardCockpitDal;
    private readonly ITglJamProvider _tglJamProvider;

    public WardRoomingInCommandHandler(
        IPakaiBedRepo pakaiBedRepo,
        IBedRepo bedRepo,
        IWardCockpitDal wardCockpitDal,
        ITglJamProvider tglJamProvider)
    {
        _pakaiBedRepo = pakaiBedRepo;
        _bedRepo = bedRepo;
        _wardCockpitDal = wardCockpitDal;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<WardRoomingInResult> Handle(WardRoomingInCommand request, CancellationToken cancellationToken)
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

        var occurredAt = SimulationTimeGuard.Resolve(
            request.TglJamIn,
            _tglJamProvider,
            operationName: "WardRoomingIn");

        var babyWaitingListBangsalId = await _wardCockpitDal.SaveRoomingInAsync(
            request.MotherPakaiBedId,
            request.BabyRegId,
            bed.BedId,
            bed.Bangsal.BangsalId,
            bed.Kamar.KamarId,
            motherPakaiBed.Reg.RegId,
            request.UserId,
            occurredAt,
            request.Catatan,
            cancellationToken);

        var result = new WardRoomingInResult(
            request.MotherPakaiBedId,
            request.BabyRegId,
            bed.BedId,
            occurredAt,
            "Active",
            babyWaitingListBangsalId
        );

        return result;
    }
}
