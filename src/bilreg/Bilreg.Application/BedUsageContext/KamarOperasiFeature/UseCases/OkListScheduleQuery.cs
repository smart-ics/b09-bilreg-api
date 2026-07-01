using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkListScheduleQuery(string TglYmd) : IRequest<IEnumerable<OkListScheduleResponse>>;

public record OkListScheduleResponse(
    string PasienId, string PasienName, string TglLahir,
    string OrderOpId, string ScheduleOpId, string NamaOperasi, string Urgency,
    int Durasi, string ScheduledTime, string StatusOp,
    string DokterId, string DokterName,
    string KamarId, string KamarName);

public class OkListScheduleHandler : IRequestHandler<OkListScheduleQuery, IEnumerable<OkListScheduleResponse>>
{
    private readonly IScheduleOpRepo _scheduleRepo;

    public OkListScheduleHandler(IScheduleOpRepo repo)
    {
        _scheduleRepo = repo;
    }

    public Task<IEnumerable<OkListScheduleResponse>> Handle(OkListScheduleQuery request, CancellationToken cancellationToken)
    {
        var listSchedule = _scheduleRepo.ListData(request.TglYmd.ToDate(DateFormatEnum.YMD))?
            .Where(x => !x.IsVoid) ?? [];
        var result = listSchedule.Select(x => new OkListScheduleResponse(
            x.Pasien.PasienId,
            x.Pasien.PasienName,
            x.Pasien.TglLahir.ToString("yyyy-MM-dd"),
            x.OrderOp.OrderOpId,
            x.ScheduleOpId,
            x.OrderOp.NamaOperasi,
            x.Urgency.ToString(),
            x.Durasi,
            x.TglOp.ToString("HH:mm"),
            TranslateState(x.Status),
            x.TeamLead.PpaId,
            x.TeamLead.PpaName,
            x.Kamar.KamarId,
            x.Kamar.KamarName
        ));
        return Task.FromResult(result);
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