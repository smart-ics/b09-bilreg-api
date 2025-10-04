using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.Helpers.CommonValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;


public class PasienTrackerModel
{
    private readonly List<PasienTrackerEventModel> _listEvent;
    public PasienTrackerModel(
        VisitorType visitor, 
        ServicePointType servicePoint, 
        ServicePointStatusEnum status, 
        IEnumerable<PasienTrackerEventModel> events)
    {
        var listEvent = events.ToList() ?? throw new ArgumentNullException(nameof(events));

        TrackerId = Ulid.NewUlid().ToString(); 
        Visitor = visitor;
        ServicePoint = servicePoint;
        Status = status;
        _listEvent = listEvent;
    }

    
    public string TrackerId { get; init; }
    public VisitorType Visitor { get; init; }
    public ServicePointType ServicePoint { get; init; }
    public ServicePointStatusEnum Status { get; init; }
    public IEnumerable<PasienTrackerEventModel> Events => _listEvent;

    
    public static PasienTrackerModel Create(PersonType person)
    {
        var visitor = new VisitorType("-", person.PersonName, person.BirthDate, "-");
        var servicePoint = new ServicePointType("-", "-", ServicePointStatusEnum.Opened, 
            AntrianModel.Default);

        return new PasienTrackerModel(visitor, servicePoint, 
            ServicePointStatusEnum.Opened, new List<PasienTrackerEventModel>());
    }

    public void AddEvent(string eventname, ReffType reffType)
    {
        var newEvent = new PasienTrackerEventModel(TrackerId, Visitor, DateTime.Now, 
            ServicePoint, AntrianStatusEnum.Waiting, reffType);
        _listEvent.Add(newEvent);
    }

    public static PasienTrackerModel Default => new PasienTrackerModel(
        VisitorType.Default,
        ServicePointType.Default,
        ServicePointStatusEnum.Opened,
        new List<PasienTrackerEventModel>());
   
}

