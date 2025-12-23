using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkOnOperationGetQuery(string ScheduleOpId) : IRequest<OnOperationGetResponse>, IScheduleOpKey;

public record OnOperationGetResponse(string ScheduleOpId, string StartOpId,
    string RegId, string PasienId, string PasienName, string TglOp, string JamMulaiOp,
    string KamarId, string KamarName, string PpaId, string DokterName);

public class OkOnOperationGetHandler : IRequestHandler<OkOnOperationGetQuery, OnOperationGetResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IStartOpRepo _startOpRepo;

    public OkOnOperationGetHandler(IScheduleOpRepo scheduleOpRepo,
        IStartOpRepo startOpRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _startOpRepo = startOpRepo;
    }

    public Task<OnOperationGetResponse> Handle(OkOnOperationGetQuery request, CancellationToken cancellationToken)
    {
        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(request.ScheduleOpId))
            .GetValueOrThrow($"Schedule Operasi ID { request.ScheduleOpId } tidak ditemukan.");

        var listStartOp = _startOpRepo.ListData(PasienModel.Key(scheduleOp.Pasien.PasienId))?.ToList() ?? [];
        var startOpWithScheduleOpId = listStartOp?
            .Where(x => !x.IsVoid).FirstOrDefault(x => x.ScheduleOpId == scheduleOp.ScheduleOpId);
        if (startOpWithScheduleOpId is null)
            throw new KeyNotFoundException($"Operasi dengan Schedule Operasi ID { request.ScheduleOpId } tidak ditemukan.");

        var startOp = _startOpRepo.LoadEntity(StartOpModel.Key(startOpWithScheduleOpId.StartOpId))
            .GetValueOrThrow($"Operasi dengan Schedule Operasi ID { request.ScheduleOpId } tidak ditemukan.");

        return Task.FromResult(GenResponse(startOp, scheduleOp));
    }

    private OnOperationGetResponse GenResponse(StartOpModel model, ScheduleOpModel schedule)
    {
        var tgl = model.StartOpTime.ToString(DateFormatEnum.YMD);
        var jam = model.StartOpTime.ToString(DateFormatEnum.HM);
        return new OnOperationGetResponse(model.ScheduleOp.ScheduleOpId, model.StartOpId,
            model.Reg.RegId, model.Pasien.PasienId, model.Pasien.PasienName, tgl, jam,
            model.KamarOp.KamarId, model.KamarOp.KamarName, schedule.TeamLead.PpaId, schedule.TeamLead.PpaName);
    }
}
