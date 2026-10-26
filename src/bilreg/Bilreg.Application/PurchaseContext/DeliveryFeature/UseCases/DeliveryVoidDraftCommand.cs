using Ardalis.GuardClauses;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryVoidDraftCommand(
    string DeliveryOrderId,
    string UserId,
    DateTime? VoidedAt = null
) : IRequest<DeliveryVoidDraftResponse>, IDeliveryOrderKey
{
    public string DoId => DeliveryOrderId;
}

public record DeliveryVoidDraftResponse(
    string DeliveryOrderId,
    string Status
)
{
    public string DoId => DeliveryOrderId;
}

public class DeliveryVoidDraftHandler : IRequestHandler<DeliveryVoidDraftCommand, DeliveryVoidDraftResponse>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public DeliveryVoidDraftHandler(
        IDeliveryOrderRepo deliveryOrderRepo,
        ITglJamProvider tglJamProvider)
    {
        _deliveryOrderRepo = deliveryOrderRepo;
        _tglJamProvider = tglJamProvider;
    }

    public DeliveryVoidDraftHandler(
        IDeliveryOrderRepo deliveryOrderRepo)
        : this(deliveryOrderRepo, new DefaultTglJamProvider())
    {
    }

    private sealed class DefaultTglJamProvider : ITglJamProvider
    {
        public DateTime Now => DateTime.Now;
    }

    public Task<DeliveryVoidDraftResponse> Handle(DeliveryVoidDraftCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.DeliveryOrderId, nameof(request.DeliveryOrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        // 2. Load Entity
        var key = DeliveryOrderModel.Key(request.DeliveryOrderId);
        var model = _deliveryOrderRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokumen penerimaan barang '{request.DeliveryOrderId}' tidak ditemukan.")
            );

        // 3. Status Transition to Voided
        var voidedAt = request.VoidedAt ?? _tglJamProvider.Now;
        model.Void(request.UserId, voidedAt);

        // 4. Save Changes (Sets TglVoid in TB_TRS_DO and Status = 'Voided' in BILRG_DoInspeksi)
        _deliveryOrderRepo.SaveChanges(model);

        return Task.FromResult(new DeliveryVoidDraftResponse(model.DeliveryOrderId, model.Status.ToString()));
    }
}
