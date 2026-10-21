using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext;

public class PasienTrackerInpatientCoordinator : IPasienTrackerInpatientCoordinator
{
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILogger<PasienTrackerInpatientCoordinator>? _logger;

    public PasienTrackerInpatientCoordinator(
        IPasienTrackerRepo trackerRepo,
        ITglJamProvider tglJamProvider,
        ILogger<PasienTrackerInpatientCoordinator>? logger = null)
    {
        _trackerRepo = trackerRepo ?? throw new ArgumentNullException(nameof(trackerRepo));
        _tglJamProvider = tglJamProvider ?? throw new ArgumentNullException(nameof(tglJamProvider));
        _logger = logger;
    }

    public PasienTrackerModel RecordReservationCreated(
        string personName,
        DateOnly tglLahir,
        DateOnly visitDate,
        string reservationId,
        DateTime occurredAt = default)
    {
        Guard.Against.NullOrWhiteSpace(personName, nameof(personName));
        Guard.Against.NullOrWhiteSpace(reservationId, nameof(reservationId));

        var timestamp = occurredAt == default ? _tglJamProvider.Now : occurredAt;
        var person = new PersonType(personName, tglLahir);
        var tracker = PasienTrackerModel.Create(
            person,
            visitDate,
            PasienTrackerInpatientEvents.Reservasi,
            reservationId,
            timestamp);

        _trackerRepo.SaveChanges(tracker);
        return tracker;
    }

    public PasienTrackerModel RecordReservationCreated(
        ReservationModel reservation,
        DateTime occurredAt = default)
    {
        Guard.Against.Null(reservation, nameof(reservation));
        var visitDate = DateOnly.FromDateTime(reservation.PlannedDate);
        return RecordReservationCreated(
            reservation.Pasien.PasienName,
            reservation.Pasien.TglLahir,
            visitDate,
            reservation.ReservationId,
            occurredAt);
    }

    public PasienTrackerModel RecordOpnameRequestCreated(
        string personName,
        DateOnly tglLahir,
        DateOnly visitDate,
        string opnameRequestId,
        DateTime occurredAt = default)
    {
        Guard.Against.NullOrWhiteSpace(personName, nameof(personName));
        Guard.Against.NullOrWhiteSpace(opnameRequestId, nameof(opnameRequestId));

        var timestamp = occurredAt == default ? _tglJamProvider.Now : occurredAt;
        var person = new PersonType(personName, tglLahir);
        var tracker = PasienTrackerModel.Create(
            person,
            visitDate,
            PasienTrackerInpatientEvents.OpnameRequest,
            opnameRequestId,
            timestamp);

        _trackerRepo.SaveChanges(tracker);
        return tracker;
    }

    public PasienTrackerModel RecordOpnameRequestCreated(
        OpnameRequestModel opnameRequest,
        DateTime occurredAt = default)
    {
        Guard.Against.Null(opnameRequest, nameof(opnameRequest));
        var visitDate = DateOnly.FromDateTime(opnameRequest.PlannedDate);
        return RecordOpnameRequestCreated(
            opnameRequest.Pasien.PasienName,
            opnameRequest.Pasien.TglLahir,
            visitDate,
            opnameRequest.OpnameRequestId,
            occurredAt);
    }

    public PasienTrackerModel RecordAdmissionRegistered(
        string personName,
        DateOnly tglLahir,
        DateOnly visitDate,
        string regId,
        string? sourceRefId = null,
        DateTime occurredAt = default)
    {
        Guard.Against.NullOrWhiteSpace(personName, nameof(personName));
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));

        var timestamp = occurredAt == default ? _tglJamProvider.Now : occurredAt;
        var person = new PersonType(personName, tglLahir);

        PasienTrackerModel? tracker = null;
        try
        {
            tracker = FindExistingTracker(person, visitDate, sourceRefId);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error occurred during PasienTracker lookup for {PersonName}. Falling back to create new tracker.", personName);
        }

        if (tracker != null)
        {
            tracker.AddEvent(PasienTrackerInpatientEvents.Registrasi, regId, timestamp);
            _trackerRepo.SaveChanges(tracker);
            return tracker;
        }

        // Graceful Fallback: tracker not found, create new tracker directly with Registrasi milestone
        var fallbackTracker = PasienTrackerModel.Create(
            person,
            visitDate,
            PasienTrackerInpatientEvents.Registrasi,
            regId,
            timestamp);

        _trackerRepo.SaveChanges(fallbackTracker);
        return fallbackTracker;
    }

    public PasienTrackerModel RecordAdmissionRegistered(
        ReservationModel reservation,
        string regId,
        DateTime occurredAt = default)
    {
        Guard.Against.Null(reservation, nameof(reservation));
        var visitDate = DateOnly.FromDateTime(reservation.PlannedDate);
        return RecordAdmissionRegistered(
            reservation.Pasien.PasienName,
            reservation.Pasien.TglLahir,
            visitDate,
            regId,
            reservation.ReservationId,
            occurredAt);
    }

    public PasienTrackerModel RecordAdmissionRegistered(
        OpnameRequestModel opnameRequest,
        string regId,
        DateTime occurredAt = default)
    {
        Guard.Against.Null(opnameRequest, nameof(opnameRequest));
        var visitDate = DateOnly.FromDateTime(opnameRequest.PlannedDate);
        return RecordAdmissionRegistered(
            opnameRequest.Pasien.PasienName,
            opnameRequest.Pasien.TglLahir,
            visitDate,
            regId,
            opnameRequest.OpnameRequestId,
            occurredAt);
    }

    private PasienTrackerModel? FindExistingTracker(PersonType person, DateOnly visitDate, string? sourceRefId)
    {
        var periode = new Periode(visitDate.ToDateTime(TimeOnly.MinValue));
        var headers = _trackerRepo.ListData(periode, person.TglLahir)?.ToList() ?? [];
        if (headers.Count == 0)
            return null;

        var candidates = headers
            .Where(h => h.Person.IsSimilar(person) ||
                        h.Person.PersonName.ToEyd().Equals(person.PersonName.ToEyd(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(sourceRefId))
        {
            foreach (var candidate in candidates)
            {
                var loaded = _trackerRepo.LoadEntity(PasienTrackerModel.Key(candidate.PasienTrackerId))
                    .Match(onSome: x => x, onNone: () => (PasienTrackerModel?)null);
                if (loaded != null && loaded.ListEvent.Any(e => string.Equals(e.ReffId, sourceRefId, StringComparison.OrdinalIgnoreCase)))
                {
                    return loaded;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(sourceRefId))
        {
            return _trackerRepo.LoadEntity(PasienTrackerModel.Key(candidates[0].PasienTrackerId))
                .Match(onSome: x => x, onNone: () => (PasienTrackerModel?)null);
        }

        return null;
    }
}
