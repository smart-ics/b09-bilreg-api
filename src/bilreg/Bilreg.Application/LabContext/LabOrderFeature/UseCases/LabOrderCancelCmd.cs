using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderCancelCmd(string OrderId, string UserId, string Reason)
    : IRequest, ILabOrderKey;

public class LabOrderCancelHandler : IRequestHandler<LabOrderCancelCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ITataRekeningRepo _tataRekeningRepo;

    public LabOrderCancelHandler(
        ILabOrderRepo labOrderRepo,
        ITglJamProvider tglJamProvider,
        ITataRekeningRepo tataRekeningRepo)
    {
        _labOrderRepo = labOrderRepo;
        _tglJamProvider = tglJamProvider;
        _tataRekeningRepo = tataRekeningRepo;
    }

    public Task Handle(LabOrderCancelCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));

        var order = _labOrderRepo.LoadEntity(request)
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        var occurredAt = _tglJamProvider.Now;

        // EX-03: Specimen Guard
        if (order.LabOrderStatus is LabOrderStatusEnum.Collected
            or LabOrderStatusEnum.Recorded
            or LabOrderStatusEnum.Verified
            or LabOrderStatusEnum.Released)
        {
            throw new InvalidOperationException(
                $"LabOrder {order.OrderId} tidak dapat dibatalkan karena spesimen telah diambil (status {order.LabOrderStatus}).");
        }

        TataRekeningModel? tataRekeningToSave = null;

        // Financial Coordination with TRK-BILLING
        if (order.LabOrderStatus == LabOrderStatusEnum.Charged)
        {
            var tataRekeningOpt = _tataRekeningRepo.LoadEntity(RegModel.Key(order.Patient.RegId));
            if (tataRekeningOpt.HasValue)
            {
                var tataRekening = tataRekeningOpt.Value;
                if (tataRekening.Status is TataRekeningStatusEnum.Finalized or TataRekeningStatusEnum.Lunas
                    || tataRekening.ListPayment.Any())
                {
                    throw new InvalidOperationException(
                        "Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");
                }

                if (tataRekening.Status == TataRekeningStatusEnum.Opened && !string.IsNullOrWhiteSpace(order.BillingTindakanId))
                {
                    if (tataRekening.ListTrsBill.Any(b => b.TrsBillingId == order.BillingTindakanId))
                    {
                        tataRekening.DeleteBill(order.BillingTindakanId);
                        tataRekeningToSave = tataRekening;
                    }
                }
            }
        }

        order.Cancel(request.UserId, request.Reason, occurredAt);

        using var trans = TransHelper.NewScope();

        if (tataRekeningToSave is not null)
        {
            _tataRekeningRepo.SaveChanges(tataRekeningToSave);
        }

        _labOrderRepo.SaveChanges(order);

        trans.Complete();

        return Task.CompletedTask;
    }
}
