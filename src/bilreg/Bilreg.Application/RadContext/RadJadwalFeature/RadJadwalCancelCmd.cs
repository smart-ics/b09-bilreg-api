using Ardalis.GuardClauses;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public record RadJadwalCancelCmd(
    string JadwalRadId,
    RadCancellationCategoryEnum Category,
    string Reason,
    string UserId) : IRequest<RadJadwalDto>, IRadJadwalKey;

public class RadJadwalCancelHandler : IRequestHandler<RadJadwalCancelCmd, RadJadwalDto>
{
    private readonly IRadJadwalRepo _radJadwalRepo;
    private readonly IRadOrderRepo _radOrderRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRadTrackerIntegration _radTrackerIntegration;

    public RadJadwalCancelHandler(
        IRadJadwalRepo radJadwalRepo,
        IRadOrderRepo radOrderRepo,
        ITglJamProvider tglJamProvider,
        IRadTrackerIntegration radTrackerIntegration)
    {
        _radJadwalRepo = radJadwalRepo;
        _radOrderRepo = radOrderRepo;
        _tglJamProvider = tglJamProvider;
        _radTrackerIntegration = radTrackerIntegration;
    }

    public async Task<RadJadwalDto> Handle(RadJadwalCancelCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.JadwalRadId, nameof(request.JadwalRadId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var jadwal = _radJadwalRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => null as RadJadwalModel);

        if (jadwal is null)
            throw new KeyNotFoundException($"Jadwal radiologi '{request.JadwalRadId}' tidak ditemukan.");

        var now = _tglJamProvider.Now;
        jadwal.Cancel(request.Category, request.Reason, request.UserId, now);

        var order = _radOrderRepo.LoadEntity(new RadOrderKey(jadwal.OrderId))
            .Match(
                onSome: x => x,
                onNone: () => null as RadOrderModel);

        var shouldUnscheduleOrder = order is not null
            && !order.AuditTrail.IsVoided
            && order.RadOrderStatus == RadOrderStatusEnum.Scheduled;

        if (shouldUnscheduleOrder)
            order!.Unschedule(request.UserId, now);

        using (var trans = TransHelper.NewScope())
        {
            _radJadwalRepo.SaveChanges(jadwal);
            if (shouldUnscheduleOrder)
                _radOrderRepo.SaveChanges(order!);
            trans.Complete();
        }

        await RadJadwalCommandSupport.TryPublishMilestoneAsync(
            _radTrackerIntegration,
            jadwal.RegId,
            jadwal.JadwalRadId,
            "Cancelled",
            $"Jadwal {jadwal.ScheduleNo} dibatalkan ({request.Category}): {request.Reason}",
            cancellationToken);

        return RadJadwalDto.FromModel(jadwal);
    }
}
