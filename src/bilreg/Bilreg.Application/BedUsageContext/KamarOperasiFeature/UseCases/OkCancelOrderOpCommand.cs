using Ardalis.GuardClauses;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkCancelOrderOpCommand : IRequest<OkCancelOrderOpResponse>, IOrderOpKey
{
    public string OrderOpId { get; init; }
    public string AlasanBatal { get; init; }
    public string UserId { get; init; }

    public OkCancelOrderOpCommand(string orderOpId, string alasanBatal, string userId = "")
    {
        Guard.Against.NullOrWhiteSpace(orderOpId, nameof(orderOpId));
        Guard.Against.NullOrWhiteSpace(alasanBatal, nameof(alasanBatal));
        OrderOpId = orderOpId;
        AlasanBatal = alasanBatal;
        UserId = userId ?? string.Empty;
    }
}

public record OkCancelOrderOpResponse(
    string OrderOpId,
    string Status,
    string AlasanBatal);

public class OkCancelOrderOpHandler : IRequestHandler<OkCancelOrderOpCommand, OkCancelOrderOpResponse>
{
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public OkCancelOrderOpHandler(
        IOrderOpRepo orderOpRepo,
        IOpCaseRepo opCaseRepo,
        IScheduleOpRepo scheduleOpRepo,
        ITglJamProvider tglJamProvider)
    {
        _orderOpRepo = orderOpRepo;
        _opCaseRepo = opCaseRepo;
        _scheduleOpRepo = scheduleOpRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<OkCancelOrderOpResponse> Handle(OkCancelOrderOpCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderOpId, nameof(request.OrderOpId));
        Guard.Against.NullOrWhiteSpace(request.AlasanBatal, nameof(request.AlasanBatal));

        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi dengan ID: {request.OrderOpId} tidak ditemukan.");

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrThrow($"OpCase dengan OrderOp ID: {request.OrderOpId} tidak ditemukan.");

        // Guard status operasi (EX-05): jika status telah mencapai OpStarted, RecoveryStarted, atau Discharged
        if (opCase.OrderOpState >= OpCaseStateEnum.OpStarted)
        {
            throw new InvalidOperationException($"Operasi dengan status {opCase.OrderOpState} tidak dapat dibatalkan.");
        }

        var now = _tglJamProvider.Now;

        // Jika status saat dibatalkan adalah Scheduled, lepaskan alokasi jadwal (Batasan §7.4)
        ScheduleOpModel? scheduleOpToCancel = null;
        if (opCase.OrderOpState == OpCaseStateEnum.Scheduled)
        {
            var scheduleId = opCase.ScheduleOp.ScheduleOpId;
            if (string.IsNullOrWhiteSpace(scheduleId) || scheduleId == "-")
            {
                var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList() ?? [];
                var activeSchedule = listSchedule.FirstOrDefault(x => !x.IsVoid && x.OrderOp.OrderOpId == request.OrderOpId);
                if (activeSchedule != null)
                {
                    scheduleId = activeSchedule.ScheduleOpId;
                }
            }

            if (!string.IsNullOrWhiteSpace(scheduleId) && scheduleId != "-")
            {
                scheduleOpToCancel = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleId))
                    .GetValueOrThrow("Schedule Operasi tidak ditemukan.");
                scheduleOpToCancel.CancelSchedule(request.UserId, now);
            }
        }

        orderOp.Cancel(request.UserId, request.AlasanBatal, now);
        opCase.CancelOrder(now);

        using var trans = TransHelper.NewScope();
        _orderOpRepo.SaveChanges(orderOp);
        _opCaseRepo.SaveChanges(opCase);
        if (scheduleOpToCancel != null)
        {
            _scheduleOpRepo.SaveChanges(scheduleOpToCancel);
        }
        trans.Complete();

        return Task.FromResult(new OkCancelOrderOpResponse(
            orderOp.OrderOpId,
            opCase.OrderOpState.ToString(),
            orderOp.AlasanBatal));
    }
}
