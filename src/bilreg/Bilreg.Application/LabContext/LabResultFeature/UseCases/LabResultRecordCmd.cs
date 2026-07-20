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

public record LabResultRecordCmd(
    string OrderId,
    string UserId,
    int ResultSource,
    IEnumerable<LabResultRecordValueDto> Values)
    : IRequest;

public class LabResultRecordHandler : IRequestHandler<LabResultRecordCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabResultDocumentRepo _labResultDocumentRepo;
    private readonly ILabResultScaffoldService _scaffoldService;
    private readonly ITglJamProvider _tglJamProvider;

    public LabResultRecordHandler(
        ILabOrderRepo labOrderRepo,
        ILabResultDocumentRepo labResultDocumentRepo,
        ILabResultScaffoldService scaffoldService,
        ITglJamProvider tglJamProvider)
    {
        _labOrderRepo = labOrderRepo;
        _labResultDocumentRepo = labResultDocumentRepo;
        _scaffoldService = scaffoldService;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(LabResultRecordCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.Null(request.Values, nameof(request.Values));

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

        var scaffold = _scaffoldService.BuildFromOrder(order);
        var captures = _scaffoldService.BuildCaptures(scaffold, request.Values);

        var occurredAt = _tglJamProvider.Now;
        var result = _labResultDocumentRepo.LoadByOrderId(request.OrderId)
            .Match(
                onSome: m => m,
                onNone: () => LabResultDocumentModel.CreateInitial(
                    request.OrderId,
                    new AuditInfoType(request.UserId, occurredAt)));

        result.RecordResult(source, captures, request.UserId, occurredAt);
        result.MarkRecorded(request.UserId, occurredAt);
        order.MarkRecorded(request.UserId, occurredAt);

        using var trans = TransHelper.NewScope();
        _labOrderRepo.SaveChanges(order);
        _labResultDocumentRepo.SaveChanges(result);
        trans.Complete();

        return Task.CompletedTask;
    }

    private sealed record OrderIdKey(string OrderId) : ILabOrderKey;
}
