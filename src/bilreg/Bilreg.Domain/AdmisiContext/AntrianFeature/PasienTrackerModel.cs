using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class PasienTrackerModel : IPasienTrackerKey
{
    private static readonly DateOnly UnsetPeriod = DateOnly.MinValue;
    private static readonly DateOnly SentinelDate = new(3000, 1, 1);

    private readonly List<PasienTrackerEventType> _listEvent;

    #region CREATION
    public PasienTrackerModel(string trackerId,
        PersonType person, DateOnly visitDate,
        DateOnly startPeriod, DateOnly lastPeriod,
        IEnumerable<PasienTrackerEventType> listEvent)
    {
        PasienTrackerId = trackerId;
        Person = person;
        VisitDate = visitDate;
        StartPeriod = startPeriod;
        LastPeriod = lastPeriod;
        _listEvent = listEvent.ToList() ?? [];
    }

    public static PasienTrackerModel Create(BookingModel booking, DateTime occurredAt = default)
    {
        var newId = Ulid.NewUlid().ToString();
        var visitor = new PersonType(booking.Person.PersonName, booking.Person.TglLahir);
        // Seed LastPeriod to VisitDate so first evidence yields max(StartPeriod, VisitDate) (BR-TRK-006).
        var result = new PasienTrackerModel(newId, visitor, booking.TglBerobat,
            UnsetPeriod, booking.TglBerobat,
            new List<PasienTrackerEventType>());
        result.AddEvent("BOOKING", booking.BookingId, occurredAt);
        return result;
    }

    public static PasienTrackerModel Create(RegModel reg, DateTime occurredAt = default)
    {
        var newId = Ulid.NewUlid().ToString();
        var visitor = new PersonType(reg.Pasien.PasienName, reg.Pasien.TglLahir);
        // Seed LastPeriod unset so first evidence yields LastPeriod = StartPeriod (BR-TRK-007).
        var result = new PasienTrackerModel(newId, visitor, reg.RegDate,
            UnsetPeriod, UnsetPeriod,
            new List<PasienTrackerEventType>());
        result.AddEvent("REGISTER", reg.RegId, occurredAt);
        return result;
    }

    /// <summary>
    /// Establish a new journey from identity snapshot and first operational evidence (BR-TRK-004, BR-TRK-025).
    /// </summary>
    public static PasienTrackerModel Create(
        PersonType person,
        DateOnly visitDate,
        string eventName,
        string reffId,
        DateTime occurredAt = default)
    {
        Guard.Against.Null(person, nameof(person));
        Guard.Against.NullOrWhiteSpace(person.PersonName, nameof(person.PersonName));

        var newId = Ulid.NewUlid().ToString();
        var result = new PasienTrackerModel(newId, person, visitDate,
            UnsetPeriod, UnsetPeriod,
            new List<PasienTrackerEventType>());
        result.AddEvent(eventName, reffId, occurredAt);
        return result;
    }

    public static PasienTrackerModel Default => new PasienTrackerModel(
        "-",
        PersonType.Default,
        SentinelDate,
        SentinelDate,
        SentinelDate,
        new List<PasienTrackerEventType>());

    public static IPasienTrackerKey Key(string id) => new PasienTrackerModel(id, PersonType.Default,
        SentinelDate, SentinelDate, SentinelDate, []);
    #endregion

    #region PROPERTIES
    public string PasienTrackerId { get; init; }
    public PersonType Person { get; init; }
    public DateOnly VisitDate { get; init; }
    public DateOnly StartPeriod { get; private set; }
    public DateOnly LastPeriod { get; private set; }
    public IEnumerable<PasienTrackerEventType> ListEvent => _listEvent;
    #endregion

    #region METHOD BEHAVIOUR
    public void AddEvent(string eventName, string reffId, DateTime occurredAt = default)
    {
        Guard.Against.NullOrWhiteSpace(eventName, nameof(eventName));
        Guard.Against.NullOrWhiteSpace(reffId, nameof(reffId));

        var isFirstEvidence = _listEvent.Count == 0;
        var eventDate = DateOnly.FromDateTime(occurredAt);

        var noUrut = _listEvent.Select(x => x.NoUrut).DefaultIfEmpty(0).Max();
        noUrut++;
        var newEvent = new PasienTrackerEventType(noUrut, eventName, occurredAt, reffId);
        _listEvent.Add(newEvent);

        if (isFirstEvidence)
            StartPeriod = eventDate;

        LastPeriod = LastPeriod == UnsetPeriod
            ? eventDate
            : Max(LastPeriod, eventDate);
    }

    private static DateOnly Max(DateOnly a, DateOnly b) => a >= b ? a : b;
    #endregion
}

public interface IPasienTrackerKey
{
    string PasienTrackerId { get; }
}
