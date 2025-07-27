namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class PasienTrackerModel
{
    public VisitorType Visitor { get; init; }
    public ServicePointType ServicePoint { get; init; }
    public ServicePointStatusEnum Status { get; init; }
}