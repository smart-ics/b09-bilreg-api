using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderUpdateCmd(
    string OrderId,
    string UserId,
    List<LabOrderTarifItemInput> Items,
    LabOrderUrgencyEnum Urgency = LabOrderUrgencyEnum.Rutin,
    string ClinicalNote = "") : IRequest<LabOrderUpdateResponse>, ILabOrderKey;

public record LabOrderUpdateResponse(string OrderId, int ItemCount, int ComponentCount);

public class LabOrderUpdateHandler : IRequestHandler<LabOrderUpdateCmd, LabOrderUpdateResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabTestResolutionService _resolutionService;
    private readonly ITglJamProvider _tglJamProvider;

    public LabOrderUpdateHandler(
        ILabOrderRepo labOrderRepo,
        ILabTestResolutionService resolutionService,
        ITglJamProvider tglJamProvider)
    {
        _labOrderRepo = labOrderRepo;
        _resolutionService = resolutionService;
        _tglJamProvider = tglJamProvider;
    }

    public Task<LabOrderUpdateResponse> Handle(LabOrderUpdateCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrEmpty(request.Items, nameof(request.Items));

        var order = _labOrderRepo.LoadEntity(request)
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        if (order.LabOrderStatus != LabOrderStatusEnum.Ordered)
            throw new InvalidOperationException(
                $"LabOrder {order.OrderId} berstatus {order.LabOrderStatus}; update hanya diperbolehkan dari Ordered.");

        var resolvedLines = LabOrderCreateHelper.MapResolvedLines(
            _resolutionService.ResolveByTarifItems(request.Items));

        var occurredAt = _tglJamProvider.Now;
        order.UpdateItems(
            resolvedLines,
            request.Urgency,
            request.ClinicalNote,
            request.UserId,
            occurredAt);

        LabOrderUpdateResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
            response = new LabOrderUpdateResponse(
                order.OrderId,
                order.Items.Count,
                order.ItemComponents.Count);
        }

        return Task.FromResult(response);
    }
}
