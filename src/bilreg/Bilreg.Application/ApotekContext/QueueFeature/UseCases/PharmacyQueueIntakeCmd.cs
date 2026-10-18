using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

/// <summary>
/// Command intake tiket antrean farmasi dari mesin kiosk mandiri.
/// </summary>
public record PharmacyQueueIntakeCmd(
    string ServicePointId,
    string DepoFarmasiId = "",
    string UserId = "KIOSK") : IRequest<PharmacyQueueIntakeResponse>
{
    public PharmacyQueueIntakeCmd(PharmacyQueueIntakeReq req)
        : this(req.ServicePointId, req.DepoFarmasiId, string.IsNullOrWhiteSpace(req.UserId) ? "KIOSK" : req.UserId)
    {
    }
}

/// <summary>
/// Handler untuk use case alokasi on-demand tiket antrean farmasi di kiosk mandiri.
/// Mengalokasikan nomor antrean sekuensial on-demand via ISequencer tanpa membuat baris OPEN,
/// membuat entri generik BILRG_AntrianEntry (Waiting = 0) dan entri apotek BILRG_AptQueueEntry (ApotekPhase = TAKEN),
/// serta menyiarkan notifikasi SignalR ke worklist Loket 1.
/// </summary>
public class PharmacyQueueIntakeHandler : IRequestHandler<PharmacyQueueIntakeCmd, PharmacyQueueIntakeResponse>
{
    private readonly IPharmacyServicePointRepo _servicePoints;
    private readonly IApotekQueueEntryRepo _queueEntries;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly ISequencer _sequencer;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;

    public PharmacyQueueIntakeHandler(
        IPharmacyServicePointRepo servicePoints,
        IApotekQueueEntryRepo queueEntries,
        IAntrianRepo antrianRepo,
        IAntrianFactory antrianFactory,
        ISequencer sequencer,
        IPharmacyQueuePublisher publisher,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth)
    {
        _servicePoints = servicePoints;
        _queueEntries = queueEntries;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _sequencer = sequencer;
        _publisher = publisher;
        _clock = clock;
        _auth = auth;
    }

    public async Task<PharmacyQueueIntakeResponse> Handle(
        PharmacyQueueIntakeCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueIntakeCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.ServicePointId, nameof(request.ServicePointId));

        var occurredAt = _clock.Now;
        var businessDate = DateOnly.FromDateTime(occurredAt);

        var servicePoint = _servicePoints.Get(request.ServicePointId)
            .GetValueOrThrow($"Pharmacy service point '{request.ServicePointId}' not found");

        if (!servicePoint.Active)
            throw new InvalidOperationException($"Pharmacy service point '{servicePoint.ServicePointId}' is not active.");

        var reference = new ServicePointType(servicePoint.ServicePointId, servicePoint.DisplayName);
        var sequenceTag = AntrianModel.GenSequenceTag(businessDate, TimeOnly.MinValue, reference);

        var queue = ResolveOrCreateQueue(servicePoint, businessDate, sequenceTag);

        PharmacyQueueIntakeResponse response;
        try
        {
            response = CommitIntake(queue, servicePoint, sequenceTag, occurredAt);
        }
        catch (AdmissionQueueSessionRaceException)
        {
            // One-winner reload jika terjadi balapan pembentukan header sesi antrean
            var winner = ReloadWinnerSession(businessDate, sequenceTag);
            response = CommitIntake(winner, servicePoint, sequenceTag, occurredAt);
        }

        await _publisher.PublishWorklistRefresh(servicePoint.DepoFarmasiId);
        return response;
    }

    private AntrianModel ResolveOrCreateQueue(
        PharmacyServicePointModel servicePoint,
        DateOnly businessDate,
        string sequenceTag)
    {
        var existingView = _antrianRepo.ListData(businessDate)
            .FirstOrDefault(x => x.SequenceTag == sequenceTag);

        if (existingView is not null)
            return _antrianRepo.LoadEntity(existingView).Value;

        var reference = new ServicePointType(servicePoint.ServicePointId, servicePoint.DisplayName);
        return _antrianFactory.Create(reference, businessDate);
    }

    private AntrianModel ReloadWinnerSession(DateOnly businessDate, string sequenceTag)
    {
        var winnerView = _antrianRepo.ListData(businessDate)
            .FirstOrDefault(x => x.SequenceTag == sequenceTag)
            ?? throw new AdmissionQueueConcurrencyException(
                $"Daily queue session for '{sequenceTag}' was created concurrently but could not be reloaded.");

        return _antrianRepo.LoadEntity(winnerView)
            .GetValueOrThrow($"Admission queue session '{winnerView.AntrianId}' not found after concurrent create.");
    }

    private PharmacyQueueIntakeResponse CommitIntake(
        AntrianModel queue,
        PharmacyServicePointModel servicePoint,
        string sequenceTag,
        DateTime occurredAt)
    {
        var genericEntry = queue.AddEntry(occurredAt);

        var aptEntry = ApotekQueueEntryModel.Create(
            queue.AntrianId,
            genericEntry.NoUrut,
            servicePoint.ServicePointId,
            servicePoint.QueuePrefix,
            occurredAt);

        using var trans = TransHelper.NewScope();
        _antrianRepo.SaveNewEntry(queue, genericEntry);
        _queueEntries.Save(aptEntry);
        trans.Complete();

        var waitingAheadCount = _queueEntries.ListByPhase(servicePoint.DepoFarmasiId, ApotekPhaseEnum.TAKEN)
            .Count(x => x.ServicePointId == servicePoint.ServicePointId && x.NoUrut < aptEntry.NoUrut);

        return new PharmacyQueueIntakeResponse(
            AntrianId: queue.AntrianId,
            NoUrut: aptEntry.NoUrut,
            QueueLabel: aptEntry.QueueLabel,
            ServicePointId: servicePoint.ServicePointId,
            ServicePointName: servicePoint.DisplayName,
            QueuePrefix: servicePoint.QueuePrefix,
            ApotekPhase: aptEntry.ApotekPhase,
            TakenAt: aptEntry.TakenAt,
            WaitingAheadCount: waitingAheadCount);
    }
}
