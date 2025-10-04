using Bilreg.Domain.Helpers.CommonValueObjects;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class PasienTrackerEventModel
{
    public PasienTrackerEventModel()
    {
    }
    public PasienTrackerEventModel(
        string trackerId,
        VisitorType visitor,
        DateTime eventTime,
        ServicePointType servicePoint,
        AntrianStatusEnum status,
        ReffType reff)
    {
        TrackerId = trackerId;
        Visitor = visitor;
        EventTime = eventTime;
        ServicePoint = servicePoint;
        Status = status;
        Refference = reff;
    }

    public string TrackerId { get; init; }
    public VisitorType Visitor { get; init; }
    public DateTime EventTime { get; init; }
    public ServicePointType ServicePoint { get; init; }
    public AntrianStatusEnum Status { get; init; }
    public ReffType Refference { get; init; }


    public static PasienTrackerEventModel Default => new()
    {
        TrackerId = "-",
        Visitor = VisitorType.Default,
        EventTime = DateTime.Now,
        ServicePoint = ServicePointType.Default,
        Status = AntrianStatusEnum.Waiting,
        Refference = ReffType.Default
    };
}