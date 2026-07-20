using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabResultFeature.UseCases;

public record LabResultAmendCmd(
    string OrderId,
    string Reason,
    string AmendedBy)
    : IRequest;

public class LabResultAmendHandler : IRequestHandler<LabResultAmendCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabResultDocumentRepo _labResultDocumentRepo;
    private readonly ILabResultScaffoldService _scaffoldService;
    private readonly ITglJamProvider _tglJamProvider;

    public LabResultAmendHandler(
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

    public Task Handle(LabResultAmendCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        Guard.Against.NullOrWhiteSpace(request.AmendedBy, nameof(request.AmendedBy));

        var order = _labOrderRepo.LoadEntity(new OrderIdKey(request.OrderId))
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        if (order.LabOrderStatus != LabOrderStatusEnum.Verified
            && order.LabOrderStatus != LabOrderStatusEnum.Released)
            throw new InvalidOperationException(
                $"LabOrder '{request.OrderId}' berstatus {order.LabOrderStatus}; amend hanya diperbolehkan saat Verified atau Released.");

        var current = _labResultDocumentRepo.LoadByOrderId(request.OrderId)
            .GetValueOrThrow($"LabResultDocument untuk order '{request.OrderId}' tidak ditemukan.");

        if (current.ResultStatus != LabResultStatusEnum.Verified)
            throw new InvalidOperationException(
                $"Hasil untuk order '{request.OrderId}' berstatus {current.ResultStatus}; amend hanya diperbolehkan untuk hasil Verified.");

        var scaffold = _scaffoldService.BuildFromOrder(order);
        var regeneratedItems = _scaffoldService.BuildStructureOnlyItems(scaffold);

        var amendedAt = _tglJamProvider.Now;
        var (retired, newVersion) = current.AmendVerifiedToNewVersion(
            regeneratedItems,
            request.Reason,
            request.AmendedBy,
            amendedAt);

        order.ReturnToRecordedAfterResultAmendment(request.AmendedBy, amendedAt);

        using var trans = TransHelper.NewScope();
        _labOrderRepo.SaveChanges(order);
        _labResultDocumentRepo.SaveChanges(retired);
        _labResultDocumentRepo.SaveChanges(newVersion);
        trans.Complete();

        return Task.CompletedTask;
    }

    private sealed record OrderIdKey(string OrderId) : ILabOrderKey;
}
