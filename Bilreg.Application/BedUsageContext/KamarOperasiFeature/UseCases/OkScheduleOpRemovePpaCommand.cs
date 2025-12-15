using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkScheduleOpRemovePpaCommand(string OrderOpId, string PpaId, string UserId) :
    IRequest, IOrderOpKey, IPpaKey;

public class OkScheduleOpRemovePpaHandler : IRequestHandler<OkScheduleOpRemovePpaCommand>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IPpaRepo _ppaRepo;

    public OkScheduleOpRemovePpaHandler(IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IPpaRepo ppaRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _ppaRepo = ppaRepo;
    }

    public Task Handle(OkScheduleOpRemovePpaCommand request, CancellationToken cancellationToken)
    {
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var ppa = _ppaRepo.LoadEntity(PpaType.Key(request.PpaId))
            .GetValueOrDefault();

        if (ppa is null)
            return Task.CompletedTask;

        var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList()
            ?? [];
        var scheduleWithOrderOpId = listSchedule?
            .Where(x => !x.IsVoid)
            .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);
        if (scheduleWithOrderOpId == null)
            return Task.CompletedTask;

        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId))
            .GetValueOrDefault();
        if (scheduleOp is null)
            return Task.CompletedTask;

        scheduleOp.RemovePpa(ppa, request.UserId);

        _scheduleOpRepo.SaveChanges(scheduleOp);

        return Task.CompletedTask;
    }
}
