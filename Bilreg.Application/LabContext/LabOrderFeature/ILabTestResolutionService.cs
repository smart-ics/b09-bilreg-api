using Bilreg.Domain.LabContext.LabOrderFeature;

namespace Bilreg.Application.LabContext.LabOrderFeature;

public record ResolvedLabOrderLine(
    LabOrderItemModel Item,
    IReadOnlyList<LabOrderItemComponentModel> Components);

public interface ILabTestResolutionService
{
    IReadOnlyList<ResolvedLabOrderLine> ResolveByTarifItems(IEnumerable<LabOrderTarifItemInput> items);
}
