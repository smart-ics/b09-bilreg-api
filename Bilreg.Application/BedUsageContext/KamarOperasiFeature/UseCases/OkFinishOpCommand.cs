using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkFinishOpCommand(string ScheduleOpId, string Tgl,
    string Jam, string UserId) : IRequest, IScheduleOpKey;

public class OkFinishOpHandler : IRequestHandler<OkFinishOpCommand>
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

    public Task Handle(OkFinishOpCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(request.ScheduleOpId))
            .GetValueOrThrow($"Schedule Operasi ID {request.ScheduleOpId} tidak ditemukan.");

        if (scheduleOp.OrderOp.OrderOpId == "-")
            throw new KeyNotFoundException($"Schedule Operasi ID {request.ScheduleOpId} tidak punya order.");

        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(scheduleOp.OrderOp.OrderOpId))
            .GetValueOrThrow($"Schedule Operasi ID {request.ScheduleOpId} tidak punya order.");

        var tglOp = string.IsNullOrWhiteSpace(request.Tgl) ? DateTime.ParseExact(
            $"{scheduleOp.TglOp.ToString(DateFormatEnum.YMD)} {request.Jam}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) :
            DateTime.ParseExact($"{ request.Tgl } {request.Jam}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrDefault()
            ?? OpCaseModel.Create(orderOp);
        opCase.Finish(tglOp);

        //  WRITE
        using var trans = TransHelper.NewScope();
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return Task.CompletedTask;
    }
}
