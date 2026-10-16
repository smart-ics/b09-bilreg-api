using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;

public record PakaiBedReleaseCommand(
    string PakaiBedId,
    DateTime TglJamOut,
    string Reason,
    string PetugasOut) : IRequest, IPakaiBedAlokasiKey;

public class PakaiBedReleaseCommandHandler : IRequestHandler<PakaiBedReleaseCommand>
{
    private readonly IPakaiBedAlokasiRepo _pakaiBedAlokasiRepo;
    private readonly IBedOperationalRepo _bedOperationalRepo;

    public PakaiBedReleaseCommandHandler(
        IPakaiBedAlokasiRepo pakaiBedAlokasiRepo,
        IBedOperationalRepo bedOperationalRepo)
    {
        _pakaiBedAlokasiRepo = pakaiBedAlokasiRepo;
        _bedOperationalRepo = bedOperationalRepo;
    }

    public Task Handle(PakaiBedReleaseCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.PakaiBedId, nameof(request.PakaiBedId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        Guard.Against.NullOrWhiteSpace(request.PetugasOut, nameof(request.PetugasOut));

        var alokasi = _pakaiBedAlokasiRepo.LoadEntity(PakaiBedAlokasiModel.Key(request.PakaiBedId))
            .GetValueOrThrow($"PakaiBed alokasi {request.PakaiBedId} tidak ditemukan.");

        var releasedAlokasi = alokasi.Release(request.TglJamOut, request.Reason, request.PetugasOut);

        var bedOp = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(alokasi.BedId))
            .GetValueOrThrow($"Bed operational {alokasi.BedId} tidak ditemukan.");

        var now = DateTime.UtcNow;
        var recordedAt = now >= request.TglJamOut ? now : request.TglJamOut;

        var updatedBedOp = bedOp.SetCleaningRequired(
            request.Reason,
            request.PetugasOut,
            request.TglJamOut,
            recordedAt,
            alokasi.PakaiBedId,
            "");

        _pakaiBedAlokasiRepo.SaveChanges(releasedAlokasi);
        _bedOperationalRepo.SaveChanges(updatedBedOp);

        return Task.CompletedTask;
    }
}
