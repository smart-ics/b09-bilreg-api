using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class PasienTrackerModel : IPasienTrackerKey
{
    private readonly List<PasienTrackerEventType> _listEvent;

    #region CREATION
    public PasienTrackerModel(string trackerId,
        PersonType person,  DateOnly visitDate, 
        IEnumerable<PasienTrackerEventType> listEvent)
    {
        PasienTrackerId = trackerId; 
        Person = person;
        VisitDate = visitDate;
        _listEvent = listEvent.ToList() ?? [];;
    }
    public static PasienTrackerModel Create(BookingModel booking, DateTime occurredAt = default)
    {
        var newId = Ulid.NewUlid().ToString();
        var visitor = new PersonType(booking.Person.PersonName, booking.Person.TglLahir);
        var result = new PasienTrackerModel(newId, visitor, booking.TglBerobat, 
            new List<PasienTrackerEventType>());
        result.AddEvent("BOOKING", booking.BookingId, occurredAt);
        return result;
    }

    public static PasienTrackerModel Create(RegModel reg, DateTime occurredAt = default)
    {
        var newId = Ulid.NewUlid().ToString();
        var visitor = new PersonType(reg.Pasien.PasienName, reg.Pasien.TglLahir);
        var result = new PasienTrackerModel(newId, visitor, reg.RegDate, 
            new List<PasienTrackerEventType>());
        result.AddEvent("REGISTER", reg.RegId, occurredAt);
        return result;
    }
    public static PasienTrackerModel Default => new PasienTrackerModel(
        "-",
        PersonType.Default, 
        new DateOnly(3000,1,1),
        new List<PasienTrackerEventType>());
    #endregion

    public static IPasienTrackerKey Key(string id) => new PasienTrackerModel(id, PersonType.Default,
        new DateOnly(3000, 1, 1), []);
    
    #region PROPERTIES
    public string PasienTrackerId { get; init; }
    public PersonType Person { get; init; }
    public DateOnly VisitDate { get; init; }
    public IEnumerable<PasienTrackerEventType> ListEvent => _listEvent;
    #endregion

    #region METHOD BEHAVIOUR
    public void AddEvent(string eventName, string reffId, DateTime occurredAt = default)
    {
        Guard.Against.NullOrWhiteSpace(eventName, nameof(eventName));
        Guard.Against.NullOrWhiteSpace(reffId, nameof(reffId));
        var noUrut = _listEvent.Select(x => x.NoUrut).DefaultIfEmpty(0).Max();
        noUrut++;
        var newEvent = new PasienTrackerEventType(noUrut, eventName, occurredAt, reffId);
        _listEvent.Add(newEvent);
    }
    #endregion
}

public interface IPasienTrackerKey
{
    string PasienTrackerId { get; }
}


