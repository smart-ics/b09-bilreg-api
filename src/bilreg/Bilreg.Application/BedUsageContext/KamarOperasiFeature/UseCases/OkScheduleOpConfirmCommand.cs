using Ardalis.GuardClauses;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkScheduleOpConfirmCommand(string OrderOpId, string UserId) : IRequest, IOrderOpKey;

public record OkScheduleOpConfirmedEvent(ScheduleOpModel Aggregate, DateTime OccurredAt) : INotification;

public class OkScheduleOpConfirmHandler : IRequestHandler<OkScheduleOpConfirmCommand>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;

    public OkScheduleOpConfirmHandler(
        IScheduleOpRepo scheduleOpRepo,
        IOpCaseRepo opCaseRepo,
        IMediator mediator,
        ITglJamProvider tglJamProvider)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _opCaseRepo = opCaseRepo;
        _mediator = mediator;
        _tglJamProvider = tglJamProvider;
    }

    public async Task Handle(OkScheduleOpConfirmCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderOpId, nameof(request.OrderOpId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var occurredAt = _tglJamProvider.Now;

        // 1. Memuat OpCaseModel
        var opCase = _opCaseRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"OpCase untuk Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        // 2. Memuat ScheduleOpModel
        ScheduleOpModel? scheduleOp = null;
        if (opCase.ScheduleOp != null && !string.IsNullOrWhiteSpace(opCase.ScheduleOp.ScheduleOpId) && opCase.ScheduleOp.ScheduleOpId != "-")
        {
            scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(opCase.ScheduleOp.ScheduleOpId)).GetValueOrDefault();
        }

        if (scheduleOp == null && opCase.Pasien != null && !string.IsNullOrWhiteSpace(opCase.Pasien.PasienId))
        {
            var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(opCase.Pasien.PasienId))?.ToList() ?? [];
            var scheduleWithOrderOpId = listSchedule
                .Where(x => !x.IsVoid)
                .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);
            if (scheduleWithOrderOpId != null)
            {
                scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId)).GetValueOrDefault();
            }
        }

        if (scheduleOp == null || scheduleOp.AuditTrail.IsVoided)
            throw new KeyNotFoundException($"Jadwal operasi untuk Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        // 3. Memvalidasi bahwa scheduleOp.IsDraft == true
        if (!scheduleOp.IsDraft)
            throw new InvalidOperationException($"Jadwal operasi untuk Order Operasi ID {request.OrderOpId} sudah berstatus resmi (bukan draf).");

        // 4. Evaluasi bentrok final zero room collision untuk kamar bersangkutan (Hard Block TD-33)
        DateTime start = scheduleOp.TglOp;
        DateTime end = start.AddMinutes(scheduleOp.Durasi);

        var activeSchedulesInRoom = _scheduleOpRepo.ListActiveSchedulesInRoom(scheduleOp.KamarOp.KamarId, start.Date)?.ToList() ?? [];

        foreach (var s in activeSchedulesInRoom)
        {
            if (s.IsVoid) continue;
            if (s.ScheduleOpId == scheduleOp.ScheduleOpId) continue;

            var startB = s.TglOp;
            var endB = s.TglOp.AddMinutes(s.Durasi);

            if (start < endB && end > startB)
            {
                throw new ScheduleCollisionException(
                    $"Bentrok jadwal kamar operasi: Kamar {scheduleOp.KamarOp.KamarId} telah terisi oleh jadwal lain dari jam {startB:HH:mm} s/d {endB:HH:mm}.");
            }
        }

        // 5. Mutasi domain model
        scheduleOp.Confirm(request.UserId, occurredAt);
        opCase.ConfirmSchedule(scheduleOp, occurredAt);

        // 6. Penyimpanan atomik di dalam TransHelper.NewScope()
        using (var trans = TransHelper.NewScope())
        {
            _scheduleOpRepo.SaveChanges(scheduleOp);
            _opCaseRepo.SaveChanges(opCase);
            trans.Complete();
        }

        // 7. Menerbitkan domain event
        await _mediator.Publish(new OkScheduleOpConfirmedEvent(scheduleOp, occurredAt), cancellationToken);
    }
}
