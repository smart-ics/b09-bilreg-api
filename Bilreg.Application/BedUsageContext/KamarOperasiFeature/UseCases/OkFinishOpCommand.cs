using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using System.Globalization;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkFinishOpCommand(string OrderOpId, string Tgl,
    string Jam, string UserId) : IRequest<OkFinishOpResponse>, IOrderOpKey;

public record OkFinishOpResponse(
    string OrderOpId,
    string ScheduleOpId,
    string RegId,
    string PasienId);

public class OkFinishOpHandler : IRequestHandler<OkFinishOpCommand, OkFinishOpResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkFinishOpHandler(IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IOpCaseRepo opCaseRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<OkFinishOpResponse> Handle(OkFinishOpCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Schedule Operasi dengan OrderOp ID: { request.OrderOpId } tidak ditemukan.");

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

        var tglOp = string.IsNullOrWhiteSpace(request.Tgl) ? DateTime.ParseExact(
            $"{scheduleOp.TglOp.ToString(DateFormatEnum.YMD)} {request.Jam}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) :
            DateTime.ParseExact($"{ request.Tgl } {request.Jam}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        opCase.Finish(tglOp);

        //  WRITE
        using var trans = TransHelper.NewScope();
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return Task.FromResult(Response(opCase));
    }

    private OkFinishOpResponse Response(OpCaseModel opCase)
    {
        return new OkFinishOpResponse(
            opCase.OrderOp.OrderOpId,
            opCase.ScheduleOp.ScheduleOpId,
            opCase.Reg.RegId,
            opCase.Pasien.PasienId
        );
    }
}
