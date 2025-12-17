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

        var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList()
            ?? [];
        var scheduleWithOrderOpId = listSchedule
            .Where(x => !x.IsVoid)
            .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);
        if (scheduleWithOrderOpId == null)
            return Task.CompletedTask;

        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId))
            .GetValueOrDefault();

        ScheduleOpModel newScheduleOp;
        if (scheduleOp != null)
        {
            scheduleOp.CancelSchedule(request.UserId);
            newScheduleOp = ScheduleOpModel.CloneFrom(scheduleOp);
        }
        else
            return Task.CompletedTask;

        // cari OldAssignedLeader di newScheduleOp.ListPpa, jika ada hapus dari newScheduleOp.ListPpa
        var oldAssignedLeader = scheduleOp.TeamLead;
        if (oldAssignedLeader != null)
        {
            var assignedPpaLead = _ppaRepo.LoadEntity(PpaType.Key(oldAssignedLeader.PpaId))
                .GetValueOrDefault();
            newScheduleOp.RemovePpa(assignedPpaLead, request.UserId);
        }

        var existingPpa = newScheduleOp.ListPpa
            .FirstOrDefault(x => x.Ppa.PpaId == request.PpaId);
        if (existingPpa is null)
            newScheduleOp.AddPpa(ppa, request.UserId);
        newScheduleOp.AssignLeader(ppa, request.UserId);

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrDefault()
            ?? OpCaseModel.Create(orderOp);
        opCase.SetListPpa(
            newScheduleOp.ListPpa
                .Select(x =>
                {
                    string profesi = x.Profesi.ProfesiName;
                    return new OpCasePpaType(x.NoUrut, x.Ppa, profesi, new DateTime(3000, 1, 1));
                }));

        using var trans = TransHelper.NewScope();
        _scheduleOpRepo.SaveChanges(scheduleOp);
        _scheduleOpRepo.SaveChanges(newScheduleOp);
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return Task.CompletedTask;
    }
}
