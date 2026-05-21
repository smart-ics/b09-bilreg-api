using System.Globalization;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

public class LabResultScaffoldService : ILabResultScaffoldService
{
    public IReadOnlyList<LabResultScaffoldLine> BuildFromOrder(LabOrderModel order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.ItemComponents.Count == 0)
            throw new InvalidOperationException(
                "LAB_RESULT_SCAFFOLD_EMPTY: LabOrder has no component snapshots; result structure cannot be generated.");

        var itemByNo = order.Items.ToDictionary(x => x.ItemNo);
        var lines = new List<LabResultScaffoldLine>();

        foreach (var component in order.ItemComponents.OrderBy(x => x.ItemNo).ThenBy(x => x.SequenceNo))
        {
            if (!itemByNo.TryGetValue(component.ItemNo, out var item))
                throw new InvalidOperationException(
                    $"LAB_RESULT_SCAFFOLD_INVALID: LabOrderItemComponent references missing ItemNo {component.ItemNo}.");

            if (!Enum.IsDefined(typeof(LabResultTypeEnum), component.ResultType))
                throw new InvalidOperationException(
                    $"LAB_RESULT_SCAFFOLD_INVALID: Unsupported ResultType {component.ResultType} on component '{component.ComponentId}'.");

            lines.Add(new LabResultScaffoldLine(
                component.ComponentId,
                component.ComponentCode,
                component.ComponentName,
                (LabResultTypeEnum)component.ResultType,
                component.Unit,
                component.ReferenceRangeText,
                component.SequenceNo,
                component.IsMandatory,
                item.TestDefinitionId,
                item.LabTestName));
        }

        return lines;
    }

    public IReadOnlyList<LabResultItemCapture> BuildCaptures(
        IReadOnlyList<LabResultScaffoldLine> scaffold,
        IEnumerable<LabResultRecordValueDto> values)
    {
        ArgumentNullException.ThrowIfNull(scaffold);
        ArgumentNullException.ThrowIfNull(values);

        if (scaffold.Count == 0)
            throw new InvalidOperationException(
                "LAB_RESULT_SCAFFOLD_EMPTY: Result scaffold has no lines.");

        var valueList = values.ToList();
        if (valueList.Count == 0)
            throw new ArgumentException("At least one result value is required.", nameof(values));

        var scaffoldById = scaffold.ToDictionary(x => x.ComponentId, StringComparer.Ordinal);
        var valueById = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in valueList)
        {
            if (string.IsNullOrWhiteSpace(entry.ComponentId))
                throw new ArgumentException("ComponentId is required on each value line.", nameof(values));

            var id = entry.ComponentId.Trim();
            if (!scaffoldById.ContainsKey(id))
                throw new InvalidOperationException(
                    $"LAB_RESULT_UNKNOWN_LINE: ComponentId '{id}' is not on the server scaffold.");

            if (!valueById.TryAdd(id, entry.Value ?? string.Empty))
                throw new InvalidOperationException(
                    $"LAB_RESULT_EXTRA_LINE: Duplicate value for ComponentId '{id}'.");
        }

        if (valueList.Count > scaffold.Count)
            throw new InvalidOperationException(
                "LAB_RESULT_EXTRA_LINE: Too many value lines submitted for the server scaffold.");

        var captures = new List<LabResultItemCapture>();
        foreach (var line in scaffold)
        {
            valueById.TryGetValue(line.ComponentId, out var rawValue);
            if (line.IsMandatory && string.IsNullOrWhiteSpace(rawValue))
                throw new InvalidOperationException(
                    $"LAB_MANDATORY_COMPONENT_MISSING: Mandatory component '{line.ComponentId}' has no value.");

            var parsed = ParseValue(line.ResultType, rawValue);

            captures.Add(new LabResultItemCapture(
                line.ComponentId,
                line.TestDefinitionId,
                line.LabTestName,
                line.ComponentCode,
                line.ComponentName,
                line.SequenceNo,
                line.ResultType,
                parsed.Numeric,
                parsed.Text,
                parsed.Option,
                parsed.Narrative,
                line.Unit,
                line.ReferenceRangeText,
                line.IsMandatory));
        }

        return captures;
    }

    public IReadOnlyList<LabResultItemModel> BuildStructureOnlyItems(
        IReadOnlyList<LabResultScaffoldLine> scaffold)
    {
        ArgumentNullException.ThrowIfNull(scaffold);

        var itemNo = 1;
        return scaffold.Select(line => new LabResultItemModel(
            itemNo++,
            line.ComponentId,
            line.TestDefinitionId,
            line.LabTestName,
            line.ComponentCode,
            line.ComponentName,
            line.SequenceNo,
            line.ResultType,
            0,
            "",
            "",
            "",
            line.Unit,
            line.ReferenceRangeText,
            line.IsMandatory,
            LabResultFlagEnum.Normal)).ToList();
    }

    private static (decimal Numeric, string Text, string Option, string Narrative) ParseValue(
        LabResultTypeEnum resultType,
        string? rawValue)
    {
        var value = rawValue?.Trim() ?? string.Empty;

        return resultType switch
        {
            LabResultTypeEnum.Numeric => ParseNumeric(value),
            LabResultTypeEnum.Text => (0, Truncate(value, 500), "", ""),
            LabResultTypeEnum.Option => (0, "", Truncate(value, 200), ""),
            LabResultTypeEnum.Narrative => (0, "", "", Truncate(value, 4000)),
            _ => throw new ArgumentException($"Unsupported ResultType '{resultType}'.")
        };
    }

    private static (decimal Numeric, string Text, string Option, string Narrative) ParseNumeric(string value)
    {
        if (value.Length == 0)
            return (0, "", "", "");

        if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var numeric))
            throw new ArgumentException($"Invalid numeric result value '{value}'.");

        return (numeric, "", "", "");
    }

    private static string Truncate(string text, int maxLen)
        => text.Length <= maxLen ? text : text[..maxLen];
}
