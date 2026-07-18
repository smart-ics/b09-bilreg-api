namespace Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

public record ExecutionDeliveryReferenceType(
    string Destination,
    string SourceFactId,
    int SourceRevision,
    string DeliveryReference);
