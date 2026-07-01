using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;

namespace Bilreg.Application.LabContext.LabResultFeature;

public interface ILabResultScaffoldService
{
    IReadOnlyList<LabResultScaffoldLine> BuildFromOrder(LabOrderModel order);

    IReadOnlyList<LabResultItemCapture> BuildCaptures(
        IReadOnlyList<LabResultScaffoldLine> scaffold,
        IEnumerable<LabResultRecordValueDto> values);

    IReadOnlyList<LabResultItemModel> BuildStructureOnlyItems(
        IReadOnlyList<LabResultScaffoldLine> scaffold);
}

public record LabResultRecordValueDto(string ComponentId, string Value);
