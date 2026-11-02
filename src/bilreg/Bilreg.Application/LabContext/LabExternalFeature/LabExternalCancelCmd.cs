using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabExternalFeature;

public record LabExternalCancelCmd(
    string RegExternalId,
    string Reason,
    string UserId) : IRequest<LabExternalCancelResponse>, ILabExternalKey;

public record LabExternalCancelResponse(
    bool Success,
    string Message);

public class LabExternalCancelHandler : IRequestHandler<LabExternalCancelCmd, LabExternalCancelResponse>
{
    private readonly ILabExternalRepo _labExternalRepo;
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public LabExternalCancelHandler(
        ILabExternalRepo labExternalRepo,
        ITataRekeningRepo tataRekeningRepo,
        ILabOrderRepo labOrderRepo,
        ITglJamProvider tglJamProvider)
    {
        _labExternalRepo = labExternalRepo;
        _tataRekeningRepo = tataRekeningRepo;
        _labOrderRepo = labOrderRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<LabExternalCancelResponse> Handle(
        LabExternalCancelCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.RegExternalId, nameof(request.RegExternalId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var labExternal = _labExternalRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"LabExternal with Id '{request.RegExternalId}' not found."));

        // EX-01: Check cashier payment on TataRekeningModel
        var hasPaidCashier = false;
        var billingId = string.IsNullOrWhiteSpace(labExternal.BillingAccountId) ? request.RegExternalId : labExternal.BillingAccountId;
        var tataRekeningOpt = _tataRekeningRepo.LoadEntity(RegModel.Key(billingId));
        if (tataRekeningOpt.HasValue)
        {
            var tataRekening = tataRekeningOpt.Value;
            if (tataRekening.ListPayment.Any() || tataRekening.Status != TataRekeningStatusEnum.Opened)
            {
                hasPaidCashier = true;
            }
        }

        // EX-02: Check specimen collection on lab orders
        var hasCollectedSpecimen = false;
        var labOrders = _labOrderRepo.ListByRegId(request.RegExternalId)?.ToList() ?? [];
        foreach (var order in labOrders)
        {
            var isCollected = (order.CollectionInfo.CollectedDate != LabOrderModel.EmptyDate && order.CollectionInfo.CollectedDate != default)
                || order.LabOrderStatus == LabOrderStatusEnum.Collected
                || order.LabOrderStatus == LabOrderStatusEnum.Recorded
                || order.LabOrderStatus == LabOrderStatusEnum.Verified
                || order.LabOrderStatus == LabOrderStatusEnum.Released
                || order.LabOrderStatus == LabOrderStatusEnum.Terminated;

            if (isCollected)
            {
                hasCollectedSpecimen = true;
                break;
            }
        }

        var now = _tglJamProvider.Now;
        labExternal.Cancel(request.Reason, request.UserId, now, hasPaidCashier, hasCollectedSpecimen);

        using var trans = TransHelper.NewScope();

        _labExternalRepo.SaveChanges(labExternal);

        foreach (var order in labOrders)
        {
            if (order.LabOrderStatus is LabOrderStatusEnum.Ordered or LabOrderStatusEnum.Deferred or LabOrderStatusEnum.Charged)
            {
                order.Cancel(request.UserId, request.Reason, now);
                _labOrderRepo.SaveChanges(order);
            }
        }

        trans.Complete();

        return Task.FromResult(new LabExternalCancelResponse(true, "Registrasi eksternal berhasil dibatalkan."));
    }
}
