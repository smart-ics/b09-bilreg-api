using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkStartOpCommand(string ScheduleOpId,
    string Jam, string UserId) : IRequest<OkStartOpResponse>, IScheduleOpKey;

public record OkStartOpResponse(
    string OrderOpId,
    string ScheduleOpId,
    string RegId,
    string PasienId);

public class OkStartOpHandler : IRequestHandler<OkStartOpCommand, OkStartOpResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkStartOpHandler(IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IOpCaseRepo opCaseRepo)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<OkStartOpResponse> Handle(OkStartOpCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        var scheduleOp = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(request.ScheduleOpId))
            .GetValueOrThrow($"Schedule Operasi ID { request.ScheduleOpId } tidak ditemukan.");

        if (scheduleOp.OrderOp.OrderOpId == "-")
            throw new KeyNotFoundException($"Schedule Operasi ID { request.ScheduleOpId } tidak punya order.");

        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(scheduleOp.OrderOp.OrderOpId))
            .GetValueOrThrow($"Schedule Operasi ID { request.ScheduleOpId } tidak punya order.");

        var tglOp = DateTime.ParseExact(
            $"{ scheduleOp.TglOp.ToString(DateFormatEnum.YMD) } { request.Jam }", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrDefault()
            ?? OpCaseModel.Create(orderOp);
        opCase.Start(tglOp);

        //  WRITE
        using var trans = TransHelper.NewScope();
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return Task.FromResult(Response(opCase));
    }

    private OkStartOpResponse Response(OpCaseModel model)
    {
        return new OkStartOpResponse(
            OrderOpId: model.OrderOpId,
            ScheduleOpId: model.ScheduleOp.ScheduleOpId,
            RegId: model.Reg.RegId,
            PasienId: model.Pasien.PasienId
        );
    }
}
