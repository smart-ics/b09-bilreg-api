namespace Bilreg.Application.LabContext.LabOrderFeature.Integration;

public record LabBillingTarifLine(string TarifId, string TarifCode, string TarifName);

public record LabBillingChargeRequest(
    string OrderId,
    string UserId,
    IReadOnlyList<LabBillingTarifLine> TarifLines);

public interface ILabBillingIntegration
{
    string CreateTindakan(LabBillingChargeRequest request);
}
