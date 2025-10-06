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
    private readonly List<PasienTrackerEventModel> _listEvent;

    #region CREATION
    public PasienTrackerModel( string trackerId,
        VisitorType visitor, 
        ServicePointType servicePoint, 
        ServicePointStatusEnum status, 
        IEnumerable<PasienTrackerEventModel> events)
    {
        var listEvent = events.ToList() ?? throw new ArgumentNullException(nameof(events));

        TrackerId = trackerId; 
        Visitor = visitor;
        ServicePoint = servicePoint;
        Status = status;
        _listEvent = listEvent;
    }

    public static PasienTrackerModel Create(PersonType person)
    {
        var newId = Ulid.NewUlid().ToString();
        var visitor = new VisitorType("-", person.PersonName, person.BirthDate, "-");
        var servicePoint = new ServicePointType("-", "-", ServicePointStatusEnum.Opened);

        return new PasienTrackerModel(newId, visitor, servicePoint,
            ServicePointStatusEnum.Opened, new List<PasienTrackerEventModel>());
    }

    public static PasienTrackerModel Default => new PasienTrackerModel(
        "-",
        VisitorType.Default,
        ServicePointType.Default,
        ServicePointStatusEnum.Opened,
        new List<PasienTrackerEventModel>());
    #endregion

    #region PROPERTIES
    public string TrackerId { get; init; }
    public VisitorType Visitor { get; init; }
    public ServicePointType ServicePoint { get; init; }
    public ServicePointStatusEnum Status { get; init; }
    public IEnumerable<PasienTrackerEventModel> Events => _listEvent;
    #endregion

    #region METHOD BEHAVIOUR
    public void AddEvent(string eventname, string reff)
    {
        var newEvent = new PasienTrackerEventModel(TrackerId, Visitor, DateTime.Now, eventname, reff);
        _listEvent.Add(newEvent);
    }
    #endregion

}


