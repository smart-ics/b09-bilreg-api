using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkScheduleOpAssignLeaderCommand(
    string OrderOpId,
    string PpaId,
    string UserId,
    string AlasanPerubahan,
    bool IsOverride = false,
    string OverrideReason = "") : IRequest, IOrderOpKey, IPpaKey;

public class OkScheduleOpAssignLeaderHandler : IRequestHandler<OkScheduleOpAssignLeaderCommand>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public OkScheduleOpAssignLeaderHandler(IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IPpaRepo ppaRepo,
        IOpCaseRepo opCaseRepo,
        ITglJamProvider tglJamProvider)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _ppaRepo = ppaRepo;
        _opCaseRepo = opCaseRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(OkScheduleOpAssignLeaderCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderOpId, nameof(request.OrderOpId));
        Guard.Against.NullOrWhiteSpace(request.PpaId, nameof(request.PpaId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.AlasanPerubahan, nameof(request.AlasanPerubahan));

        var occurredAt = _tglJamProvider.Now;
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var ppa = _ppaRepo.LoadEntity(PpaType.Key(request.PpaId))
            .GetValueOrThrow($"Ppa ID {request.PpaId} tidak ditemukan.");

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrThrow("Invalid Order Operasi. OpCase data tidak ditemukan.");

        var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList()
            ?? [];
        var scheduleWithOrderOpId = listSchedule
            .Where(x => !x.IsVoid)
            .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);
        if (scheduleWithOrderOpId == null)
            throw new KeyNotFoundException("Schedule Operasi tidak ditemukan.");

        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId))
            .GetValueOrThrow("Schedule Operasi tidak ditemukan.");

        // Collision detection for substitute doctor across active schedules on the same date
        DateTime start = scheduleOp.TglOp;
        DateTime end = start.AddMinutes(scheduleOp.Durasi);

        var activeDoctorSchedules = _scheduleOpRepo.ListActiveSchedulesForDoctor(ppa.PpaId, start.Date)?.ToList() ?? [];
        var activeTeamSchedules = _scheduleOpRepo.ListActiveSchedulesForPpaInTeam(ppa.PpaId, start.Date)?.ToList() ?? [];

        var combinedSchedules = activeDoctorSchedules
            .Concat(activeTeamSchedules)
            .GroupBy(x => x.ScheduleOpId)
            .Select(g => g.OrderByDescending(x => !string.IsNullOrWhiteSpace(x.ProfesiName)).First())
            .ToList();

        var warnings = new List<string>();
        foreach (var d in combinedSchedules)
        {
            if (d.IsVoid) continue;
            if (d.ScheduleOpId == scheduleOp.ScheduleOpId) continue;

            var startD = d.TglOp;
            var endD = d.TglOp.AddMinutes(d.Durasi);

            if (start < endD && end > startD)
            {
                var roomDisplay = !string.IsNullOrWhiteSpace(d.Kamar?.KamarId) ? d.Kamar.KamarId : scheduleOp.KamarOp?.KamarId ?? "";
                var roleInfo = !string.IsNullOrWhiteSpace(d.ProfesiName) ? $" sebagai {d.ProfesiName}" : "";
                warnings.Add($"Dokter/PPA {ppa.PpaName}{roleInfo} telah terjadwal pada operasi lain ({roomDisplay}) dari jam {startD:HH:mm} s/d {endD:HH:mm}.");
            }
        }

        if (warnings.Count > 0 && !request.IsOverride)
        {
            throw new ScheduleWarningException(warnings);
        }

        if (warnings.Count > 0 && request.IsOverride)
        {
            Guard.Against.NullOrWhiteSpace(request.OverrideReason, nameof(request.OverrideReason),
                "Alasan diskresi override wajib diisi jika terdapat peringatan bentrok dokter/turnover buffer.");
        }

        scheduleOp.AssignLeader(ppa, request.AlasanPerubahan, request.UserId, occurredAt, request.IsOverride, request.OverrideReason);

        opCase.Schedule(scheduleOp, occurredAt);

        orderOp.SetDokterOperator(ppa);
        orderOp.AuditTrail.Modif(request.UserId, occurredAt);

        using var trans = TransHelper.NewScope();
        _scheduleOpRepo.SaveChanges(scheduleOp);
        _opCaseRepo.SaveChanges(opCase);
        _orderOpRepo.SaveChanges(orderOp);
        trans.Complete();

        return Task.CompletedTask;
    }
}
