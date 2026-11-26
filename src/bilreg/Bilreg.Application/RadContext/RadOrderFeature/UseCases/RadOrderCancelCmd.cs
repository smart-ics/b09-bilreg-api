using Ardalis.GuardClauses;
using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Application.RadContext.RadOrderFeature.Integration;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.RadContext.RadOrderFeature.UseCases;

public record RadOrderCancelCmd(
    string OrderId,
    string UserId,
    string Reason) : IRequest, IRadOrderKey;

public class RadOrderCancelHandler : IRequestHandler<RadOrderCancelCmd>
{
    private readonly IRadOrderRepo _radOrderRepo;
    private readonly IRadBillingIntegration _radBillingIntegration;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRadJadwalRepo? _radJadwalRepo;
    private readonly IRadTrackerIntegration? _radTrackerIntegration;

    public RadOrderCancelHandler(
        IRadOrderRepo radOrderRepo,
        IRadBillingIntegration radBillingIntegration,
        ITglJamProvider tglJamProvider,
        IRadJadwalRepo? radJadwalRepo = null,
        IRadTrackerIntegration? radTrackerIntegration = null)
    {
        _radOrderRepo = radOrderRepo;
        _radBillingIntegration = radBillingIntegration;
        _tglJamProvider = tglJamProvider;
        _radJadwalRepo = radJadwalRepo;
        _radTrackerIntegration = radTrackerIntegration;
    }

    public async Task Handle(RadOrderCancelCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));

        var order = _radOrderRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => null as RadOrderModel
            );

        if (order is null)
            throw new KeyNotFoundException($"Order radiologi dengan OrderId '{request.OrderId}' tidak ditemukan.");

        var now = _tglJamProvider.Now;
        if (order.RadOrderStatus == RadOrderStatusEnum.Charged)
        {
            _radBillingIntegration.CancelCharge(
                order.OrderId,
                order.Patient.RegId,
                request.UserId,
                request.Reason,
                now);
        }

        order.Cancel(request.Reason, request.UserId, now);

        var activeJadwal = _radJadwalRepo?.LoadActiveByOrderId(order.OrderId)
            .Match(
                onSome: x => x,
                onNone: () => null as RadJadwalModel
            );

        if (activeJadwal is not null)
        {
            activeJadwal.Cancel(
                RadCancellationCategoryEnum.OrderDibatalkan,
                request.Reason,
                request.UserId,
                now);
        }

        using (var trans = TransHelper.NewScope())
        {
            _radOrderRepo.SaveChanges(order);
            if (activeJadwal is not null)
                _radJadwalRepo!.SaveChanges(activeJadwal);
            trans.Complete();
        }

        if (activeJadwal is not null && _radTrackerIntegration is not null)
        {
            await RadJadwalCommandSupport.TryPublishMilestoneAsync(
                _radTrackerIntegration,
                activeJadwal.RegId,
                activeJadwal.JadwalRadId,
                "Cancelled",
                $"Jadwal {activeJadwal.ScheduleNo} dibatalkan karena order dibatalkan: {request.Reason}",
                cancellationToken);
        }
    }
}
