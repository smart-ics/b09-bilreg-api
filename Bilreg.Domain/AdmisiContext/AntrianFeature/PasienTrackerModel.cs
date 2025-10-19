using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
using Bilreg.Domain.Helpers.CommonValueObjects;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using Xunit;

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
    public static PasienTrackerModel Create(PersonInfoType person)
    {
        var newId = Ulid.NewUlid().ToString();
        var visitor = new PersonType(person.PersonName, person.TglLahir);

        return new PasienTrackerModel(newId, visitor, new DateOnly(3000,1,1), new List<PasienTrackerEventType>());
    }
    public static PasienTrackerModel Default => new PasienTrackerModel(
        "-",
        PersonType.Default, 
        new DateOnly(3000,1,1),
        new List<PasienTrackerEventType>());
    #endregion

    #region PROPERTIES
    public string PasienTrackerId { get; init; }
    public PersonType Person { get; init; }
    public DateOnly VisitDate { get; init; }
    public IEnumerable<PasienTrackerEventType> ListEvent => _listEvent;
    #endregion

    #region METHOD BEHAVIOUR
    public void AddEvent(string eventName, string reffId)
    {
        Guard.Against.NullOrWhiteSpace(eventName, nameof(eventName));
        Guard.Against.NullOrWhiteSpace(reffId, nameof(reffId));
        var noUrut = _listEvent.Select(x => x.NoUrut).DefaultIfEmpty(0).Max();
        noUrut++;
        var newEvent = new PasienTrackerEventType(noUrut, eventName, DateTime.Now, reffId);
        _listEvent.Add(newEvent);
    }
    #endregion
}

public interface IPasienTrackerKey
{
    string PasienTrackerId { get; }
}


