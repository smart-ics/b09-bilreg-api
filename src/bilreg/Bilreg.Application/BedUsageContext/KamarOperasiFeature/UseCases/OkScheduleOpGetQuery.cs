using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkScheduleOpGetQuery(string OrderOpId) : IRequest<ScheduleOpGetResponse>, IOrderOpKey;

public record ScheduleOpGetResponse(
    string OrderOpId,
    string ScheduleOpId,
    string NamaOperasi,
    string Urgency,
    string PasienId,
    string PasienName,
    string RegId,
    string TglOp,
    string JamOp,
    string TglStartOp,
    string JamStartOp,
    string TglFinishOp,
    string JamFinishOp,
    string KamarId,
    string KamarName,
    string DokterId,
    string DokterName,
    string StatusOp,
    IEnumerable<PpaReff> ListPpa,
    string AlasanBatal,
    int TurnoverBuffer,
    IEnumerable<OkScheduleOpHistDto> History);

public record OkScheduleOpHistDto(
    int NoUrut,
    DateTime OldTglOp,
    DateTime NewTglOp,
    int OldDurasi,
    int NewDurasi,
    string OldKamarId,
    string OldKamarName,
    string NewKamarId,
    string NewKamarName,
    string OldTeamLeadId,
    string OldTeamLeadName,
    string NewTeamLeadId,
    string NewTeamLeadName,
    string Alasan,
    bool IsOverride,
    string OverrideReason,
    string UserId,
    DateTime CrtDate);

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
        var scheduleWithOrderOpId = listSchedule
            .Where(x => !x.IsVoid)
            .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);
        if (scheduleWithOrderOpId is null)
            throw new KeyNotFoundException($"Schedule Operasi untuk Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId))
            .GetValueOrThrow($"Schedule Operasi untuk Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var historyList = scheduleOp.ListHistory?.Select(h => new OkScheduleOpHistDto(
            h.NoUrut,
            h.OldTglOp,
            h.NewTglOp,
            h.OldDurasi,
            h.NewDurasi,
            h.OldKamar?.KamarId ?? string.Empty,
            h.OldKamar?.KamarName ?? string.Empty,
            h.NewKamar?.KamarId ?? string.Empty,
            h.NewKamar?.KamarName ?? string.Empty,
            h.OldTeamLead?.PpaId ?? string.Empty,
            h.OldTeamLead?.PpaName ?? string.Empty,
            h.NewTeamLead?.PpaId ?? string.Empty,
            h.NewTeamLead?.PpaName ?? string.Empty,
            h.Alasan,
            h.IsOverride,
            h.OverrideReason,
            h.UserId,
            h.CrtDate
        )).ToList() ?? [];

        var response = new ScheduleOpGetResponse(
            scheduleOp.OrderOp.OrderOpId,
            scheduleOp.ScheduleOpId,
            orderOp.NamaOperasi,
            orderOp.UrgencyLevel.ToString(),
            scheduleOp.Pasien.PasienId,
            scheduleOp.Pasien.PasienName,
            scheduleOp.Reg.RegId,
            scheduleOp.TglOp.ToString(DateFormatEnum.YMD),
            scheduleOp.TglOp.ToString(DateFormatEnum.HM),
            scheduleOp.StartOpDate.ToString(DateFormatEnum.YMD),
            scheduleOp.StartOpDate.ToString(DateFormatEnum.HM),
            scheduleOp.EndOpDate.ToString(DateFormatEnum.YMD),
            scheduleOp.EndOpDate.ToString(DateFormatEnum.HM),
            scheduleOp.KamarOp.KamarId,
            scheduleOp.KamarOp.KamarName,
            scheduleOp.TeamLead.PpaId,
            scheduleOp.TeamLead.PpaName,
            TranslateState(scheduleOp.OrderOpState),
            scheduleOp.ListPpa.Select(x => new PpaReff(x.Ppa.PpaId, x.Ppa.PpaName)),
            scheduleOp.AlasanBatal,
            scheduleOp.TurnoverBuffer,
            historyList);

        return Task.FromResult(response);
    }

    private string TranslateState(OpCaseStateEnum opCaseState)
    {
        string result = opCaseState switch
        {
            OpCaseStateEnum.Scheduled => opCaseState.ToString(),
            OpCaseStateEnum.OpStarted => "In-Progress",
            OpCaseStateEnum.RecoveryStarted => "In-Recovery",
            _ => "Others"
        };
        return result;
    }
}
