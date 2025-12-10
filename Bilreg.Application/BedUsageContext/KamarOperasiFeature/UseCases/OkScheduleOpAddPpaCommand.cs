using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using System.Globalization;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkScheduleOpAddPpaCommand(string OrderOpId, string PpaId, string UserId) : IRequest, IOrderOpKey, IPpaKey;

public class OkScheduleOpAddPpaHandler : IRequestHandler<OkScheduleOpAddPpaCommand>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IPpaRepo _ppaRepo;

    public OkScheduleOpAddPpaHandler(IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IPpaRepo ppaRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _ppaRepo = ppaRepo;
    }

    public Task Handle(OkScheduleOpAddPpaCommand request, CancellationToken cancellationToken)
    {
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var ppa = _ppaRepo.LoadEntity(PpaType.Key(request.PpaId))
            .GetValueOrThrow($"Ppa ID {request.PpaId} tidak ditemukan.");

        var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList()
            ?? [];
        var scheduleWithOrderOpId = listSchedule?
            .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId)
            ?? new ScheduleOpView("-", new PasienReff("-", "-", DateOnly.ParseExact("3000-01-01", "yyyy-MM-dd"), "-"),
            new OrderOpReff("-", DateTime.ParseExact("3000-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "-"),
            UrgencyLevelEnum.Elective, DateTime.ParseExact("3000-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            0, new PpaReff("-", "-"), new KamarReff("-", "-"));

        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId))
            .GetValueOrDefault();

        if (scheduleOp is null)
            return Task.CompletedTask;

        scheduleOp.AddPpa(ppa, request.UserId);

        _scheduleOpRepo.SaveChanges(scheduleOp);

        return Task.CompletedTask;
    }
}
