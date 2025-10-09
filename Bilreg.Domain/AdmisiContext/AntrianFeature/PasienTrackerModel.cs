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


public class PasienTrackerModel
{
    private readonly List<PasienTrackerEventType> _listEvent;

    #region CREATION
    public PasienTrackerModel(string trackerId,
        VisitorType visitor,  IEnumerable<PasienTrackerEventType> listEvent)
    {
        TrackerId = trackerId; 
        Visitor = visitor;
        _listEvent = listEvent.ToList() ?? [];;
    }
    public static PasienTrackerModel Create(PersonType person)
    {
        var newId = Ulid.NewUlid().ToString();
        var visitor = new VisitorType(person.PersonName, DateOnly.FromDateTime(person.BirthDate), DateTime.Now);

        return new PasienTrackerModel(newId, visitor, new List<PasienTrackerEventType>());
    }
    public static PasienTrackerModel Default => new PasienTrackerModel(
        "-",
        VisitorType.Default,
        new List<PasienTrackerEventType>());
    #endregion

    #region PROPERTIES
    public string TrackerId { get; init; }
    public VisitorType Visitor { get; init; }
    public IEnumerable<PasienTrackerEventType> Events => _listEvent;
    #endregion

    #region METHOD BEHAVIOUR
    public void AddEvent(string eventName, string reffId)
    {
        Guard.Against.NullOrWhiteSpace(eventName, nameof(eventName));
        Guard.Against.NullOrWhiteSpace(reffId, nameof(reffId));
        var newEvent = new PasienTrackerEventType(eventName, DateTime.Now, reffId);
        _listEvent.Add(newEvent);
    }
    #endregion
}


