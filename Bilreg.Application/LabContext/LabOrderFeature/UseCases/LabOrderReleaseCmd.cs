using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderReleaseCmd(string OrderId, string UserId, string ReleaseNote)
    : IRequest<LabOrderReleaseResponse>, ILabOrderKey;

public record LabOrderReleaseResponse(
    bool Released,
    string BillingStatus,
    string Message);

public class LabOrderReleaseHandler : IRequestHandler<LabOrderReleaseCmd, LabOrderReleaseResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabBillingIntegration _labBillingIntegration;
    private readonly ILabBillingReleaseCheckDal _billingReleaseCheckDal;

    public LabOrderReleaseHandler(
        ILabOrderRepo labOrderRepo,
        ILabBillingIntegration labBillingIntegration,
        ILabBillingReleaseCheckDal billingReleaseCheckDal)
    {
        _labOrderRepo = labOrderRepo;
        _labBillingIntegration = labBillingIntegration;
        _billingReleaseCheckDal = billingReleaseCheckDal;
    }

    public Task<LabOrderReleaseResponse> Handle(LabOrderReleaseCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.Null(request.ReleaseNote, nameof(request.ReleaseNote));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        if (order.AuditTrail.IsVoided)
            throw new InvalidOperationException($"LabOrder {order.OrderId} sudah void; release tidak diperbolehkan.");

        if (order.LabOrderStatus == LabOrderStatusEnum.Released)
            throw new InvalidOperationException($"LabOrder {order.OrderId} sudah Released; release bersifat final.");

        if (order.LabOrderStatus != LabOrderStatusEnum.Verified)
            throw new InvalidOperationException(
                $"LabOrder {order.OrderId} berstatus {order.LabOrderStatus}; release hanya diperbolehkan saat Verified.");

        var validation = _labBillingIntegration.ValidateReleaseEligibility(
            new LabBillingReleaseValidationRequest(order.OrderId, order.OrderNo, request.UserId));

        var checkedAt = DateTime.Now;
        var check = BillingReleaseCheckModel.Create(
            order.OrderId,
            validation.Status,
            validation.Message,
            request.UserId,
            checkedAt);

        if (validation.Status == BillingReleaseStatusEnum.Blocked)
        {
            using (var trans = TransHelper.NewScope())
            {
                _billingReleaseCheckDal.Insert(check);
                trans.Complete();
            }

            return Task.FromResult(new LabOrderReleaseResponse(
                Released: false,
                BillingStatus: BillingReleaseStatusApi.Blocked,
                Message: validation.Message));
        }

        order.Release(request.UserId, request.ReleaseNote);

        LabOrderReleaseResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _billingReleaseCheckDal.Insert(check);
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
            response = new LabOrderReleaseResponse(
                Released: true,
                BillingStatus: BillingReleaseStatusApi.Clear,
                Message: "");
        }

        return Task.FromResult(response);
    }
}
