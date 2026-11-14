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

        var isPa = order.ServiceType == LaboratoryServiceType.AnatomicalPathology
            || order.Items.Any(i => i.DisciplineId.StartsWith("DISC-PA", StringComparison.OrdinalIgnoreCase));

        var isMicro = order.ServiceType == LaboratoryServiceType.Microbiology
            || order.Items.Any(i => i.DisciplineId.Equals("DISC-MIK", StringComparison.OrdinalIgnoreCase));

        if (isPa && !order.ItemComponents.Any(c => c.ComponentCode.StartsWith("PA_", StringComparison.OrdinalIgnoreCase)))
        {
            var primaryItem = order.Items.FirstOrDefault();
            var testDefId = primaryItem?.TestDefinitionId ?? "PA-01";
            var testName = primaryItem?.LabTestName ?? "Patologi Anatomi";
            var lines = new List<LabResultScaffoldLine>
            {
                new("PA_REG_NO", "PA_REG_NO", "Nomor Registrasi PA", LabResultTypeEnum.Text, "", "", 1, false, testDefId, testName),
                new("PA_SLIDE_NO", "PA_SLIDE_NO", "Nomor Sediaan / Slide", LabResultTypeEnum.Text, "", "", 2, false, testDefId, testName),
                new("PA_SPECIMEN_SITE", "PA_SPECIMEN_SITE", "Asal / Lokasi Jaringan", LabResultTypeEnum.Text, "", "", 3, false, testDefId, testName),
                new("PA_MACROSCOPIC", "PA_MACROSCOPIC", "Pemeriksaan Makroskopis", LabResultTypeEnum.Narrative, "", "", 4, false, testDefId, testName),
                new("PA_SPECIMEN_ADEQUACY", "PA_SPECIMEN_ADEQUACY", "Kecukupan Sediaan (Adekuasi)", LabResultTypeEnum.Narrative, "", "", 5, false, testDefId, testName),
                new("PA_MICROSCOPIC", "PA_MICROSCOPIC", "Pemeriksaan Mikroskopis", LabResultTypeEnum.Narrative, "", "", 6, false, testDefId, testName),
                new("PA_CONCLUSION", "PA_CONCLUSION", "Kesimpulan / Diagnosis Patologi", LabResultTypeEnum.Narrative, "", "", 7, false, testDefId, testName),
                new("PA_RECOMMENDATION", "PA_RECOMMENDATION", "Rekomendasi Klinis", LabResultTypeEnum.Narrative, "", "", 8, false, testDefId, testName),
                new("PA_SNOMED", "PA_SNOMED", "Kode Morfologi (ICD-O / SNOMED)", LabResultTypeEnum.Text, "", "", 9, false, testDefId, testName),
            };

            foreach (var comp in order.ItemComponents)
            {
                if (!lines.Any(l => l.ComponentCode.Equals(comp.ComponentCode, StringComparison.OrdinalIgnoreCase)))
                {
                    lines.Add(new LabResultScaffoldLine(
                        comp.ComponentId,
                        comp.ComponentCode,
                        comp.ComponentName,
                        (LabResultTypeEnum)comp.ResultType,
                        comp.Unit,
                        comp.ReferenceRangeText,
                        lines.Count + 1,
                        false,
                        testDefId,
                        testName));
                }
            }

            return lines;
        }

        if (isMicro && !order.ItemComponents.Any(c => c.ComponentCode.StartsWith("MIC_", StringComparison.OrdinalIgnoreCase)))
        {
            var primaryItem = order.Items.FirstOrDefault();
            var testDefId = primaryItem?.TestDefinitionId ?? "MIC-01";
            var testName = primaryItem?.LabTestName ?? "Mikrobiologi";
            var lines = new List<LabResultScaffoldLine>
            {
                new("MIC_DIRECT_SMEAR", "MIC_DIRECT_SMEAR", "Pemeriksaan Mikroskopis Langsung", LabResultTypeEnum.Text, "", "", 1, false, testDefId, testName),
                new("MIC_GROWTH_STATUS", "MIC_GROWTH_STATUS", "Pertumbuhan Kuman", LabResultTypeEnum.Option, "", "Tidak Ada Pertumbuhan", 2, false, testDefId, testName),
                new("MIC_COLONY_COUNT", "MIC_COLONY_COUNT", "Hitung Koloni", LabResultTypeEnum.Text, "CFU/mL", "", 3, false, testDefId, testName),
                new("MIC_ISOLATED_ORGANISM", "MIC_ISOLATED_ORGANISM", "Organisme yang Ditemukan", LabResultTypeEnum.Text, "", "", 4, false, testDefId, testName),
            };

            foreach (var comp in order.ItemComponents)
            {
                if (!lines.Any(l => l.ComponentCode.Equals(comp.ComponentCode, StringComparison.OrdinalIgnoreCase)))
                {
                    lines.Add(new LabResultScaffoldLine(
                        comp.ComponentId,
                        comp.ComponentCode,
                        comp.ComponentName,
                        (LabResultTypeEnum)comp.ResultType,
                        comp.Unit,
                        comp.ReferenceRangeText,
                        lines.Count + 1,
                        false,
                        testDefId,
                        testName));
                }
            }

            return lines;
        }

        if (order.ItemComponents.Count == 0)
            throw new InvalidOperationException(
                "LAB_RESULT_SCAFFOLD_EMPTY: LabOrder has no component snapshots; result structure cannot be generated.");

        var itemByNo = order.Items.ToDictionary(x => x.ItemNo);
        var stdLines = new List<LabResultScaffoldLine>();

        foreach (var component in order.ItemComponents.OrderBy(x => x.ItemNo).ThenBy(x => x.SequenceNo))
        {
            if (!itemByNo.TryGetValue(component.ItemNo, out var item))
                throw new InvalidOperationException(
                    $"LAB_RESULT_SCAFFOLD_INVALID: LabOrderItemComponent references missing ItemNo {component.ItemNo}.");

            if (!Enum.IsDefined(typeof(LabResultTypeEnum), component.ResultType))
                throw new InvalidOperationException(
                    $"LAB_RESULT_SCAFFOLD_INVALID: Unsupported ResultType {component.ResultType} on component '{component.ComponentId}'.");

            stdLines.Add(new LabResultScaffoldLine(
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

        return stdLines;
    }

    public IReadOnlyList<LabResultItemCapture> BuildCaptures(
        IReadOnlyList<LabResultScaffoldLine> scaffold,
        IEnumerable<LabResultRecordValueDto> values,
        IEnumerable<LabResultRecordItemInputDto>? explicitItems = null)
    {
        ArgumentNullException.ThrowIfNull(scaffold);

        if (scaffold.Count == 0)
            throw new InvalidOperationException(
                "LAB_RESULT_SCAFFOLD_EMPTY: Result scaffold has no lines.");

        var explicitList = explicitItems?.ToList();
        if (explicitList != null && explicitList.Count > 0)
        {
            return BuildCapturesFromExplicitItems(scaffold, explicitList);
        }

        ArgumentNullException.ThrowIfNull(values);
        var valueList = values.ToList();
        if (valueList.Count == 0)
            throw new ArgumentException("At least one result value is required.", nameof(values));

        var scaffoldById = scaffold.ToDictionary(x => x.ComponentId, StringComparer.Ordinal);
        var scaffoldByCode = scaffold
            .Where(x => !string.IsNullOrWhiteSpace(x.ComponentCode))
            .ToLookup(x => x.ComponentCode, StringComparer.OrdinalIgnoreCase);
        var valueById = new Dictionary<string, string>(StringComparer.Ordinal);
        var dynamicAstValues = new List<(string Code, string Value)>();

        foreach (var entry in valueList)
        {
            if (string.IsNullOrWhiteSpace(entry.ComponentId))
                throw new ArgumentException("ComponentId is required on each value line.", nameof(values));

            var key = entry.ComponentId.Trim();
            if (scaffoldById.ContainsKey(key))
            {
                if (!valueById.TryAdd(key, entry.Value ?? string.Empty))
                    throw new InvalidOperationException(
                        $"LAB_RESULT_EXTRA_LINE: Duplicate value for ComponentId '{key}'.");
            }
            else
            {
                var match = scaffoldByCode[key].FirstOrDefault();
                if (match is not null)
                {
                    if (!valueById.TryAdd(match.ComponentId, entry.Value ?? string.Empty))
                        throw new InvalidOperationException(
                            $"LAB_RESULT_EXTRA_LINE: Duplicate value for ComponentId '{match.ComponentId}'.");
                }
                else if (key.StartsWith("AST_", StringComparison.OrdinalIgnoreCase) || key.StartsWith("AST", StringComparison.OrdinalIgnoreCase))
                {
                    dynamicAstValues.Add((key, entry.Value ?? string.Empty));
                }
                else
                {
                    throw new InvalidOperationException(
                        $"LAB_RESULT_UNKNOWN_LINE: ComponentId '{key}' is not on the server scaffold.");
                }
            }
        }

        if (valueById.Count > scaffold.Count)
            throw new InvalidOperationException(
                "LAB_RESULT_EXTRA_LINE: Too many value lines submitted for the server scaffold.");

        var isPaOrMicroScaffold = scaffold.Any(s =>
            s.ComponentCode.StartsWith("PA_", StringComparison.OrdinalIgnoreCase) ||
            s.ComponentCode.StartsWith("MIC_", StringComparison.OrdinalIgnoreCase));

        var captures = new List<LabResultItemCapture>();
        foreach (var line in scaffold)
        {
            var hasValue = valueById.TryGetValue(line.ComponentId, out var rawValue);
            if (line.IsMandatory && string.IsNullOrWhiteSpace(rawValue))
                throw new InvalidOperationException(
                    $"LAB_MANDATORY_COMPONENT_MISSING: Mandatory component '{line.ComponentId}' has no value.");

            // Do not record optional PA / Micro scaffold lines that were not submitted
            if (!hasValue && (isPaOrMicroScaffold ||
                              line.ComponentCode.StartsWith("PA_", StringComparison.OrdinalIgnoreCase) ||
                              line.ComponentCode.StartsWith("MIC_", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

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

        foreach (var ast in dynamicAstValues)
        {
            var testDefId = scaffold.FirstOrDefault()?.TestDefinitionId ?? "MIC-01";
            var testName = scaffold.FirstOrDefault()?.LabTestName ?? "Mikrobiologi";
            captures.Add(new LabResultItemCapture(
                ast.Code,
                testDefId,
                testName,
                ast.Code,
                $"AST: {ast.Code}",
                captures.Count + 1,
                LabResultTypeEnum.Option,
                0m,
                "",
                ast.Value,
                "",
                "",
                "",
                false));
        }

        return captures;
    }

    private static IReadOnlyList<LabResultItemCapture> BuildCapturesFromExplicitItems(
        IReadOnlyList<LabResultScaffoldLine> scaffold,
        List<LabResultRecordItemInputDto> explicitList)
    {
        var scaffoldById = scaffold.ToDictionary(x => x.ComponentId, StringComparer.Ordinal);
        var scaffoldByCode = scaffold
            .Where(x => !string.IsNullOrWhiteSpace(x.ComponentCode))
            .ToLookup(x => x.ComponentCode, StringComparer.OrdinalIgnoreCase);

        var explicitByCode = explicitList
            .Where(x => !string.IsNullOrWhiteSpace(x.ComponentCode))
            .ToDictionary(x => x.ComponentCode!.Trim(), StringComparer.OrdinalIgnoreCase);

        var isPaOrMicroScaffold = scaffold.Any(s =>
            s.ComponentCode.StartsWith("PA_", StringComparison.OrdinalIgnoreCase) ||
            s.ComponentCode.StartsWith("MIC_", StringComparison.OrdinalIgnoreCase));

        var captures = new List<LabResultItemCapture>();
        var matchedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in scaffold)
        {
            explicitByCode.TryGetValue(line.ComponentCode, out var explicitItem);
            if (explicitItem is null && !string.IsNullOrWhiteSpace(line.ComponentId))
                explicitByCode.TryGetValue(line.ComponentId, out explicitItem);

            if (explicitItem is not null)
            {
                matchedCodes.Add(explicitItem.ComponentCode ?? line.ComponentCode);

                var rawFallback = explicitItem.Value ?? explicitItem.OptionValue ?? explicitItem.NarrativeValue ?? explicitItem.TextValue ?? explicitItem.NumericValue?.ToString();
                var parsed = ParseValue(line.ResultType, rawFallback);

                var numeric = explicitItem.NumericValue ?? (line.ResultType == LabResultTypeEnum.Numeric ? parsed.Numeric : null);
                var text = explicitItem.TextValue ?? (line.ResultType == LabResultTypeEnum.Text ? parsed.Text : "");
                var option = explicitItem.OptionValue ?? (line.ResultType == LabResultTypeEnum.Option ? parsed.Option : "");
                var narrative = explicitItem.NarrativeValue ?? (line.ResultType == LabResultTypeEnum.Narrative ? parsed.Narrative : "");

                var hasValue = (numeric != null && numeric != 0) || !string.IsNullOrWhiteSpace(text) || !string.IsNullOrWhiteSpace(option) || !string.IsNullOrWhiteSpace(narrative);
                if (line.IsMandatory && !hasValue)
                    throw new InvalidOperationException($"LAB_MANDATORY_COMPONENT_MISSING: Mandatory component '{line.ComponentId}' has no value.");

                captures.Add(new LabResultItemCapture(
                    line.ComponentId,
                    !string.IsNullOrWhiteSpace(explicitItem.TestId) ? explicitItem.TestId : line.TestDefinitionId,
                    !string.IsNullOrWhiteSpace(explicitItem.TestName) ? explicitItem.TestName : line.LabTestName,
                    line.ComponentCode,
                    !string.IsNullOrWhiteSpace(explicitItem.ComponentName) ? explicitItem.ComponentName : line.ComponentName,
                    line.SequenceNo,
                    line.ResultType,
                    numeric ?? 0m,
                    text,
                    option,
                    narrative,
                    !string.IsNullOrWhiteSpace(explicitItem.Unit) ? explicitItem.Unit : line.Unit,
                    !string.IsNullOrWhiteSpace(explicitItem.ReferenceRangeText) ? explicitItem.ReferenceRangeText : line.ReferenceRangeText,
                    line.IsMandatory));
            }
            else
            {
                if (line.IsMandatory)
                    throw new InvalidOperationException($"LAB_MANDATORY_COMPONENT_MISSING: Mandatory component '{line.ComponentId}' has no value.");

                if (isPaOrMicroScaffold ||
                    line.ComponentCode.StartsWith("PA_", StringComparison.OrdinalIgnoreCase) ||
                    line.ComponentCode.StartsWith("MIC_", StringComparison.OrdinalIgnoreCase) ||
                    line.ComponentCode.Equals("REMARK", StringComparison.OrdinalIgnoreCase) ||
                    line.ComponentCode.Equals("GRAM_STN", StringComparison.OrdinalIgnoreCase) ||
                    line.ComponentId.Equals("MLC000C", StringComparison.OrdinalIgnoreCase) ||
                    line.ComponentId.Equals("MLC00A2", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                captures.Add(new LabResultItemCapture(
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
                    line.IsMandatory));
            }
        }

        foreach (var item in explicitList)
        {
            var code = item.ComponentCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code) || matchedCodes.Contains(code))
                continue;

            var isDynamic = code.StartsWith("AST_", StringComparison.OrdinalIgnoreCase)
                || code.StartsWith("AST", StringComparison.OrdinalIgnoreCase);

            if (!isDynamic && !scaffoldByCode.Contains(code) && !scaffoldById.ContainsKey(code))
                throw new InvalidOperationException($"LAB_RESULT_UNKNOWN_LINE: ComponentId '{code}' is not on the server scaffold.");

            var resultType = item.ResultType.HasValue && Enum.IsDefined(typeof(LabResultTypeEnum), item.ResultType.Value)
                ? (LabResultTypeEnum)item.ResultType.Value
                : LabResultTypeEnum.Option;

            captures.Add(new LabResultItemCapture(
                ComponentId: code,
                TestId: item.TestId ?? (scaffold.FirstOrDefault()?.TestDefinitionId ?? "MIC-01"),
                TestName: item.TestName ?? (scaffold.FirstOrDefault()?.LabTestName ?? "Mikrobiologi"),
                ComponentCode: code,
                ComponentName: item.ComponentName ?? code,
                SequenceNo: captures.Count + 1,
                ResultType: resultType,
                NumericValue: item.NumericValue ?? 0m,
                TextValue: item.TextValue ?? "",
                OptionValue: item.OptionValue ?? "",
                NarrativeValue: item.NarrativeValue ?? "",
                Unit: item.Unit ?? "",
                ReferenceRangeText: item.ReferenceRangeText ?? "",
                IsMandatory: false));
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
