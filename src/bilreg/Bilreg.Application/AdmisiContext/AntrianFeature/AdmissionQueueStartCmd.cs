using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record AdmissionQueueStartCmd(
    string AntrianId,
    int NoUrut,
    string UserId) : IRequest<AdmissionQueueStartResponse>;

public record AdmissionQueueStartResponse(
    string AntrianId,
    int NoUrut,
    string Status,
    DateTime ServedAt);

public class AdmissionQueueStartHandler
    : IRequestHandler<AdmissionQueueStartCmd, AdmissionQueueStartResponse>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IAdmissionServicePointResolver _servicePointResolver;

    public AdmissionQueueStartHandler(
        IAntrianRepo antrianRepo,
        ITglJamProvider tglJamProvider,
        IAdmissionServicePointResolver servicePointResolver)
    {
        _antrianRepo = antrianRepo;
        _tglJamProvider = tglJamProvider;
        _servicePointResolver = servicePointResolver;
    }

    public Task<AdmissionQueueStartResponse> Handle(
        AdmissionQueueStartCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AntrianId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.NoUrut <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.NoUrut));

        AdmissionQueueStartResponse response;
        using (var trans = TransHelper.NewScope())
        {
            var queue = _antrianRepo.LoadEntity(AntrianModel.Key(request.AntrianId))
                .GetValueOrThrow($"Admission queue '{request.AntrianId}' not found");
            _servicePointResolver.EnsureAdmissionQueue(queue);
            var entry = AdmissionQueueIdentify.RequireAnonymousWaitingEntry(queue, request.NoUrut);
            entry.Serve(_tglJamProvider.Now);
            if (!_antrianRepo.TrySaveWaitingToInServiceTransition(queue, entry))
                throw new AdmissionQueueConcurrencyException(
                    $"Queue entry '{queue.AntrianId}' / {entry.NoUrut} was changed concurrently.");
            trans.Complete();

            response = new AdmissionQueueStartResponse(
                queue.AntrianId,
                entry.NoUrut,
                entry.AntrianStatus.ToString(),
                entry.ServedAt);
        }

        return Task.FromResult(response);
    }
}
