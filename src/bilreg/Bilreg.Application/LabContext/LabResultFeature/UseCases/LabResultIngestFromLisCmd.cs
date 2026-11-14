using System.Globalization;
using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabResultFeature.UseCases;

public record LabResultIngestItemDto(
    string ComponentCode,
    string Value,
    string Unit = "",
    string ReferenceRangeText = "",
    string Flag = "");

public record LabResultIngestFromLisCmd(
    string OrderId,
    string LisRefNo,
    string ValidatorName,
    DateTime ValidatedDate,
    IEnumerable<LabResultIngestItemDto> Items,
    string CriticalNote = "") : IRequest
{
    public string OrderNo => OrderId;

    public LabResultIngestFromLisCmd(
        string orderId,
        string lisRefNo,
        string validatorName,
        DateTime validatedDate,
        string criticalNote,
        IEnumerable<LabResultIngestItemDto> items)
        : this(orderId, lisRefNo, validatorName, validatedDate, items, criticalNote)
    {
    }
}

public class LabResultIngestFromLisHandler : IRequestHandler<LabResultIngestFromLisCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabResultDocumentRepo _labResultDocumentRepo;
    private readonly ILabResultScaffoldService _scaffoldService;
    private readonly ITglJamProvider? _tglJamProvider;

    public LabResultIngestFromLisHandler(
        ILabOrderRepo labOrderRepo,
        ILabResultDocumentRepo labResultDocumentRepo,
        ILabResultScaffoldService scaffoldService,
        ITglJamProvider? tglJamProvider = null)
    {
        _labOrderRepo = labOrderRepo;
        _labResultDocumentRepo = labResultDocumentRepo;
        _scaffoldService = scaffoldService;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(LabResultIngestFromLisCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.ValidatorName, nameof(request.ValidatorName));
        Guard.Against.Null(request.Items, nameof(request.Items));

        var validatedDate = request.ValidatedDate != default
            ? request.ValidatedDate
            : _tglJamProvider?.Now ?? DateTime.Now;

        var order = _labOrderRepo.LoadEntity(new OrderIdKey(request.OrderId))
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        if (order.LabOrderStatus != LabOrderStatusEnum.Charged
            && order.LabOrderStatus != LabOrderStatusEnum.Collected
            && order.LabOrderStatus != LabOrderStatusEnum.Recorded
            && order.LabOrderStatus != LabOrderStatusEnum.Verified)
        {
            throw new InvalidOperationException(
                $"LabOrder '{request.OrderId}' berstatus {order.LabOrderStatus}; ingesti hasil LIS tidak diperbolehkan.");
        }

        var scaffold = _scaffoldService.BuildFromOrder(order);

        var incomingByCode = request.Items
            .Where(x => !string.IsNullOrWhiteSpace(x.ComponentCode))
            .GroupBy(x => x.ComponentCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);

        var existingDoc = _labResultDocumentRepo.LoadByOrderId(request.OrderId);
        var existingItemByComponentId = existingDoc.HasValue
            ? existingDoc.Value.Items.ToDictionary(x => x.ComponentId, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, LabResultItemModel>(StringComparer.OrdinalIgnoreCase);

        var resultItems = new List<LabResultItemModel>();
        var itemNo = 1;

        foreach (var line in scaffold)
        {
            var compCode = line.ComponentCode.Trim();
            if (incomingByCode.TryGetValue(compCode, out var lisItem))
            {
                var unit = !string.IsNullOrWhiteSpace(lisItem.Unit) ? lisItem.Unit.Trim() : line.Unit;
                var refRange = !string.IsNullOrWhiteSpace(lisItem.ReferenceRangeText)
                    ? lisItem.ReferenceRangeText.Trim()
                    : line.ReferenceRangeText;

                var (numVal, textVal, optVal, narrVal) = ParseIncomingValue(line.ResultType, lisItem.Value);

                var flag = MapLisFlag(lisItem.Flag)
                    ?? LabResultFlagging.Compute(line.ResultType, numVal, refRange);

                resultItems.Add(new LabResultItemModel(
                    itemNo++,
                    line.ComponentId,
                    line.TestDefinitionId,
                    line.LabTestName,
                    line.ComponentCode,
                    line.ComponentName,
                    line.SequenceNo,
                    line.ResultType,
                    numVal,
                    textVal,
                    optVal,
                    narrVal,
                    unit,
                    refRange,
                    line.IsMandatory,
                    flag));
            }
            else if (existingItemByComponentId.TryGetValue(line.ComponentId, out var existingItem) && HasValue(existingItem))
            {
                resultItems.Add(new LabResultItemModel(
                    itemNo++,
                    line.ComponentId,
                    line.TestDefinitionId,
                    line.LabTestName,
                    line.ComponentCode,
                    line.ComponentName,
                    line.SequenceNo,
                    line.ResultType,
                    existingItem.NumericValue,
                    existingItem.TextValue,
                    existingItem.OptionValue,
                    existingItem.NarrativeValue,
                    existingItem.Unit,
                    existingItem.ReferenceRangeText,
                    line.IsMandatory,
                    existingItem.FlagStatus));
            }
            else
            {
                // Partial result: unreceived scaffold component remains empty
                resultItems.Add(new LabResultItemModel(
                    itemNo++,
                    line.ComponentId,
                    line.TestDefinitionId,
                    line.LabTestName,
                    line.ComponentCode,
                    line.ComponentName,
                    line.SequenceNo,
                    line.ResultType,
                    0m,
                    "",
                    "",
                    "",
                    line.Unit,
                    line.ReferenceRangeText,
                    line.IsMandatory,
                    LabResultFlagEnum.Normal));
            }
        }

        var expertiseNote = existingDoc.HasValue ? existingDoc.Value.ExpertiseNote : "";
        var audit = new AuditInfoType(request.ValidatorName, validatedDate);
        var resultDoc = LabResultDocumentModel.CreateFromLisIngestion(
            request.OrderId,
            resultItems,
            request.ValidatorName,
            validatedDate,
            audit,
            request.CriticalNote,
            expertiseNote);

        // Update order status to Verified
        if (order.LabOrderStatus == LabOrderStatusEnum.Charged || order.LabOrderStatus == LabOrderStatusEnum.Collected)
        {
            order.MarkRecorded(request.ValidatorName, validatedDate);
        }

        if (order.LabOrderStatus == LabOrderStatusEnum.Recorded)
        {
            order.MarkVerified(request.ValidatorName, validatedDate);
        }
        else if (order.LabOrderStatus == LabOrderStatusEnum.Verified)
        {
            order.AuditTrail.Modif(request.ValidatorName, validatedDate);
        }

        using var trans = TransHelper.NewScope();
        if (existingDoc.HasValue)
        {
            _labResultDocumentRepo.DeleteEntity(existingDoc.Value);
        }
        _labOrderRepo.SaveChanges(order);
        _labResultDocumentRepo.SaveChanges(resultDoc);
        trans.Complete();

        return Task.CompletedTask;
    }

    private static LabResultFlagEnum? MapLisFlag(string? flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
            return null;

        var trimmed = flag.Trim();
        return trimmed.ToUpperInvariant() switch
        {
            "L" or "LOW" or "<" => LabResultFlagEnum.Low,
            "H" or "HIGH" or ">" => LabResultFlagEnum.High,
            "C" or "CRIT" or "CRITICAL" or "PANIC" => LabResultFlagEnum.Critical,
            "N" or "NORM" or "NORMAL" => LabResultFlagEnum.Normal,
            _ => null
        };
    }

    private static (decimal Numeric, string Text, string Option, string Narrative) ParseIncomingValue(
        LabResultTypeEnum resultType,
        string? rawValue)
    {
        var val = rawValue?.Trim() ?? string.Empty;
        return resultType switch
        {
            LabResultTypeEnum.Numeric => TryParseNumeric(val),
            LabResultTypeEnum.Text => (0m, Truncate(val, 500), "", ""),
            LabResultTypeEnum.Option => (0m, "", Truncate(val, 200), ""),
            LabResultTypeEnum.Narrative => (0m, "", "", Truncate(val, 4000)),
            _ => (0m, Truncate(val, 500), "", "")
        };
    }

    private static (decimal Numeric, string Text, string Option, string Narrative) TryParseNumeric(string val)
    {
        if (string.IsNullOrWhiteSpace(val))
            return (0m, "", "", "");

        if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) ||
            decimal.TryParse(val, NumberStyles.Any, CultureInfo.CurrentCulture, out num))
        {
            return (num, "", "", "");
        }

        return (0m, Truncate(val, 500), "", "");
    }

    private static string Truncate(string text, int maxLen)
        => text.Length <= maxLen ? text : text[..maxLen];

    private static bool HasValue(LabResultItemModel item)
    {
        return item.ResultType switch
        {
            LabResultTypeEnum.Numeric => item.NumericValue != 0m || !string.IsNullOrWhiteSpace(item.TextValue),
            LabResultTypeEnum.Text => !string.IsNullOrWhiteSpace(item.TextValue),
            LabResultTypeEnum.Option => !string.IsNullOrWhiteSpace(item.OptionValue),
            LabResultTypeEnum.Narrative => !string.IsNullOrWhiteSpace(item.NarrativeValue),
            _ => !string.IsNullOrWhiteSpace(item.TextValue) || item.NumericValue != 0m
        };
    }

    private sealed record OrderIdKey(string OrderId) : ILabOrderKey;
}
