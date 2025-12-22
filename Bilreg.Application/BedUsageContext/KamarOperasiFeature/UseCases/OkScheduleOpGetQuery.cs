using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkScheduleOpGetQuery(string OrderOpId) : IRequest<ScheduleOpGetResponse>, IOrderOpKey;

public record ScheduleOpGetResponse(string OrderOpId, string ScheduleOpId, string NamaOperasi, string Urgency,
    string PasienId, string PasienName, string RegId, string TglOp, string JamOp,
    string KamarId, string KamarName,
    string DokterId, string DokterName,
    IEnumerable<PpaReff> ListPpa);

public class OkScheduleOpGetHandler : IRequestHandler<OkScheduleOpGetQuery, ScheduleOpGetResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;

    public OkScheduleOpGetHandler(IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
    }

    public Task<ScheduleOpGetResponse> Handle(OkScheduleOpGetQuery request, CancellationToken cancellationToken)
    {
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList()
            ?? [];
        var scheduleWithOrderOpId = listSchedule?
            .Where(x => !x.IsVoid)
            .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);
        if (scheduleWithOrderOpId is null)
            throw new KeyNotFoundException($"Schedule Operasi untuk Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId))
            .GetValueOrThrow($"Schedule Operasi untuk Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var response = new ScheduleOpGetResponse(scheduleOp.OrderOp.OrderOpId, scheduleOp.ScheduleOpId,
            orderOp.NamaOperasi,
            orderOp.UrgencyLevel.ToString(),
            scheduleOp.Pasien.PasienId,
            scheduleOp.Pasien.PasienName,
            scheduleOp.Reg.RegId,
            scheduleOp.TglOp.ToString(DateFormatEnum.YMD),
            scheduleOp.TglOp.ToString(DateFormatEnum.HM),
            scheduleOp.KamarOp.KamarId,
            scheduleOp.KamarOp.KamarName,
            scheduleOp.TeamLead.PpaId,
            scheduleOp.TeamLead.PpaName,
            scheduleOp.ListPpa.Select(x => new PpaReff(x.Ppa.PpaId, x.Ppa.PpaName)));
        return Task.FromResult(response);
    }
}
