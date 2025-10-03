using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;


public class PasienTrackerModel
{
    public PasienTrackerModel(VisitorType visitor, 
        ServicePointType servicePoint, 
        ServicePointStatusEnum status)
    {
        Visitor = visitor;
        ServicePoint = servicePoint;
        Status = status;
    }

    public VisitorType Visitor { get; init; }
    public ServicePointType ServicePoint { get; init; }
    public ServicePointStatusEnum Status { get; init; }
}

public static PasienTrackerModel Create(
        VisitorType visitor,
        ServicePointType servicePoint,
        ServicePointStatusEnum status)
    {
        Guard.Against.Null(visitor, nameof(visitor));
        Guard.Against.Null(servicePoint, nameof(servicePoint));
        Guard.Against.Null(status, nameof(status));

        return new PasienTrackerModel(visitor, servicePoint, status);
    }

    public static PasienTrackerModel Load(
        VisitorType visitor,
        ServicePointType servicePoint,
        ServicePointStatusEnum status)
        => new PasienTrackerModel(visitor, servicePoint, status);

    // Nilai default
    public static PasienTrackerModel Default => new PasienTrackerModel(
        VisitorType.Default,
        ServicePointType.Default,
        ServicePointStatusEnum.Opened 
    );