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
        string eventName,
        string reff)
    {
        TrackerId = trackerId;
        Visitor = visitor;
        EventTime = eventTime;
        EventName = eventName;
        Refference = reff;
    }

    public string TrackerId { get; init; }
    public VisitorType Visitor { get; init; }
    public DateTime EventTime { get; init; }
    public string EventName { get; init; }
    public string Refference { get; init; }


    public static PasienTrackerEventModel Default => new()
    {
        TrackerId = "-",
        Visitor = VisitorType.Default,
        EventTime = DateTime.Now,
        EventName = "-",
        Refference = "-"
    };
}