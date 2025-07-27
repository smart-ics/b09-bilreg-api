namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class PasienTrackerEventModel
{
    public VisitorType Visitor { get; init; }
    public DateTime EventTime { get; init; }
    public ServicePointType ServicePoint { get; init; }
    public AntrianStatusEnum Status { get; init; }
}