using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkScheduleOpAssignLeaderCommand(string OrderOpId, string PpaId, string UserId) :
    IRequest, IOrderOpKey, IPpaKey;

public class OkScheduleOpAssignLeaderHandler : IRequestHandler<OkScheduleOpAssignLeaderCommand>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkScheduleOpAssignLeaderHandler(IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IPpaRepo ppaRepo,
        IOpCaseRepo opCaseRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _ppaRepo = ppaRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task Handle(OkScheduleOpAssignLeaderCommand request, CancellationToken cancellationToken)
    {
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

        scheduleOp.CancelSchedule(request.UserId);

        ScheduleOpModel newScheduleOp;
        newScheduleOp = ScheduleOpModel.CloneFrom(scheduleOp);

        // Checks if TeamLead is not null AND if PpaId is a valid string
        if (scheduleOp.TeamLead is { PpaId: string ppaId } oldAssignedLead &&
            !string.IsNullOrWhiteSpace(ppaId) &&
            ppaId != "-")
        {
            var assignedPpaLead = _ppaRepo.LoadEntity(PpaType.Key(ppaId))
                .GetValueOrDefault();
            newScheduleOp.RemovePpa(assignedPpaLead, request.UserId);
        }

        var existingPpa = newScheduleOp.ListPpa
            .FirstOrDefault(x => x.Ppa.PpaId == request.PpaId);
        if (existingPpa is null)
            newScheduleOp.AddPpa(ppa, request.UserId);
        newScheduleOp.AssignLeader(ppa, request.UserId);

        opCase.Schedule(newScheduleOp);

        using var trans = TransHelper.NewScope();
        _scheduleOpRepo.SaveChanges(scheduleOp);
        _scheduleOpRepo.SaveChanges(newScheduleOp);
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return Task.CompletedTask;
    }
}
