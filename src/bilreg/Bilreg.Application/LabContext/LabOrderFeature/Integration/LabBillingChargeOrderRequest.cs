using Bilreg.Domain.LabContext.LabOrderFeature;

namespace Bilreg.Application.LabContext.LabOrderFeature.Integration;

public record LabBillingChargeOrderRequest(
    LabOrderModel Order,
    string UserId,
    DateTime OccurredAt);

public record LabBillingChargeResult(
    string BillingTindakanId);
