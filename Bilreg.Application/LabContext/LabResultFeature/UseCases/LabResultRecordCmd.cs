using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabResultFeature.UseCases;

public record LabResultRecordItemDto(
    string TestId,
    string TestName,
    string ComponentCode,
    string ComponentName,
    int ResultType,
    decimal NumericValue,
    string? TextValue,
    string? OptionValue,
    string? NarrativeValue,
    string? Unit,
    string? ReferenceRangeText);

public record LabResultRecordCmd(
    string OrderId,
    string UserId,
    int ResultSource,
    IEnumerable<LabResultRecordItemDto> Items)
    : IRequest;

public class LabResultRecordHandler : IRequestHandler<LabResultRecordCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabResultDocumentRepo _labResultDocumentRepo;

    public LabResultRecordHandler(ILabOrderRepo labOrderRepo, ILabResultDocumentRepo labResultDocumentRepo)
    {
        _labOrderRepo = labOrderRepo;
        _labResultDocumentRepo = labResultDocumentRepo;
    }

    public Task Handle(LabResultRecordCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.Null(request.Items, nameof(request.Items));

        var itemList = request.Items.ToList();
        Guard.Against.NullOrEmpty(itemList, nameof(request.Items));

        var order = _labOrderRepo.LoadEntity(new OrderIdKey(request.OrderId))
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        if (order.LabOrderStatus != LabOrderStatusEnum.Charged
            && order.LabOrderStatus != LabOrderStatusEnum.Collected
            && order.LabOrderStatus != LabOrderStatusEnum.Recorded)
            throw new InvalidOperationException(
                $"LabOrder '{request.OrderId}' berstatus {order.LabOrderStatus}; rekaman hasil hanya diperbolehkan dari Charged, Collected, atau Recorded.");

        var source = (LabResultSourceEnum)request.ResultSource;
        if (!Enum.IsDefined(typeof(LabResultSourceEnum), source))
            throw new ArgumentException("ResultSource tidak valid.", nameof(request.ResultSource));

        foreach (var x in itemList)
        {
            if (!Enum.IsDefined(typeof(LabResultTypeEnum), x.ResultType))
                throw new ArgumentException("ResultType tidak valid.", nameof(request.Items));
        }

        var captures = itemList.Select(x => new LabResultItemCapture(
            x.TestId,
            x.TestName,
            x.ComponentCode ?? "",
            x.ComponentName ?? "",
            (LabResultTypeEnum)x.ResultType,
            x.NumericValue,
            x.TextValue ?? "",
            x.OptionValue ?? "",
            x.NarrativeValue ?? "",
            x.Unit ?? "",
            x.ReferenceRangeText ?? "")).ToList();

        var result = _labResultDocumentRepo.LoadByOrderId(request.OrderId)
            .Match(
                onSome: m => m,
                onNone: () => LabResultDocumentModel.CreateInitial(
                    request.OrderId,
                    new AuditInfoType(request.UserId, DateTime.Now)));

        result.RecordResult(source, captures, request.UserId);
        result.MarkRecorded(request.UserId);
        order.MarkRecorded(request.UserId);

        using var trans = TransHelper.NewScope();
        _labOrderRepo.SaveChanges(order);
        _labResultDocumentRepo.SaveChanges(result);
        trans.Complete();

        return Task.CompletedTask;
    }

    private sealed record OrderIdKey(string OrderId) : ILabOrderKey;
}
