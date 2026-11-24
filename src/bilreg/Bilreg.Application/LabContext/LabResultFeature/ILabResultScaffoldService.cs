using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;

namespace Bilreg.Application.LabContext.LabResultFeature;

public interface ILabResultScaffoldService
{
    IReadOnlyList<LabResultScaffoldLine> BuildFromOrder(LabOrderModel order);

    IReadOnlyList<LabResultItemCapture> BuildCaptures(
        IReadOnlyList<LabResultScaffoldLine> scaffold,
        IEnumerable<LabResultRecordValueDto> values,
        IEnumerable<LabResultRecordItemInputDto>? explicitItems = null);

    IReadOnlyList<LabResultItemModel> BuildStructureOnlyItems(
        IReadOnlyList<LabResultScaffoldLine> scaffold);
}

public record LabResultRecordValueDto(string ComponentId, string Value);

public record LabResultRecordItemInputDto(
    string? TestId = null,
    string? TestName = null,
    string? ComponentCode = null,
    string? ComponentName = null,
    int? ResultType = null,
    decimal? NumericValue = null,
    string? TextValue = null,
    string? OptionValue = null,
    string? NarrativeValue = null,
    string? Unit = null,
    string? ReferenceRangeText = null,
    string? Value = null);
