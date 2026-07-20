using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkScheduleOpAddPpaCommand(string OrderOpId, string PpaId, string UserId) : IRequest, IOrderOpKey, IPpaKey;

public class OkScheduleOpAddPpaHandler : IRequestHandler<OkScheduleOpAddPpaCommand>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public OkScheduleOpAddPpaHandler(IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IPpaRepo ppaRepo,
        IOpCaseRepo opCaseRepo,
        ITglJamProvider? tglJamProvider = null)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _ppaRepo = ppaRepo;
        _opCaseRepo = opCaseRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(OkScheduleOpAddPpaCommand request, CancellationToken cancellationToken)
    {
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

        scheduleOp.AddPpa(ppa, request.UserId, occurredAt);

        opCase.Schedule(scheduleOp, occurredAt);

        using var trans = TransHelper.NewScope();
        _scheduleOpRepo.SaveChanges(scheduleOp);
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();
        
        return Task.CompletedTask;
    }
}
